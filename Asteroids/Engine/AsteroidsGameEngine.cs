using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Asteroids.Engine.Audio;
using Asteroids.Engine.Entities;
using GameResources = Asteroids.Resources;
using Microsoft.Xna.Framework;
using WpfImage = System.Windows.Controls.Image;
using WpfPoint = System.Windows.Point;
using WpfVector = System.Windows.Vector;
using VelcroPhysics.Collision.Filtering;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Factories;

namespace Asteroids.Engine;

public sealed class AsteroidsGameEngine : IDisposable
{
    private readonly Canvas _canvas;
    private readonly AsteroidsGameConfig _config;
    private readonly Random _random = new();
    private readonly List<AsteroidEntity> _asteroids = new();
    private readonly List<ExplosionEntity> _explosions = new();
    private readonly List<DebrisEntity> _debris = new();
    private readonly SoundEffectPlayer _explosionAudioPlayer;
    private readonly SoundEffectPlayer _asteroidBumpAudioPlayer;
    private readonly World _physicsWorld;
    private readonly CroppedBitmap[] _asteroidFrames;
    private readonly CroppedBitmap[] _explosionFrames;
    private readonly List<Body> _monitorWallBodies = new();
    private readonly List<Rect> _monitorBounds = new();
    private WpfPoint _cursorPosition;
    private double _spawnAccumulator;
    private bool _shutdownMode;

    public AsteroidsGameEngine(Canvas canvas, AsteroidsGameConfig config)
    {
        _canvas = canvas;
        _config = config;
        _physicsWorld = new World(Microsoft.Xna.Framework.Vector2.Zero);

        BitmapSource asteroidSheet = SpriteSheetLoader.ConvertBitmap(GameResources.AsteroidsSprite);
        BitmapSource explosionSheet = SpriteSheetLoader.ConvertBitmap(GameResources.Explosion);
        _asteroidFrames = SpriteSheetLoader.Slice(asteroidSheet, config.AsteroidColumns, config.AsteroidRows);
        _explosionFrames = SpriteSheetLoader.Slice(explosionSheet, config.ExplosionColumns, config.ExplosionRows);
        _explosionAudioPlayer = new SoundEffectPlayer(GameResources.ExplosionSFX, "asteroids_explosion", config.ExplosionVolume);
        _asteroidBumpAudioPlayer = new SoundEffectPlayer(GameResources.AsteroidBump, "asteroids_bump", config.AsteroidBumpVolume, TimeSpan.FromMilliseconds(40));
    }

    public int ActiveAsteroids => _asteroids.Count;
    public int DestroyedAsteroids { get; private set; }

    public void Initialize()
    {
        _cursorPosition = new WpfPoint(_config.MinWorldWidth * 0.5, _config.MinWorldHeight * 0.5);
    }

    public void SetCursorPosition(WpfPoint cursorPosition)
    {
        _cursorPosition = cursorPosition;
    }

    public void SetMonitorBounds(IReadOnlyList<Rect> monitorBounds)
    {
        _monitorBounds.Clear();
        _monitorBounds.AddRange(monitorBounds);
        RebuildMonitorWalls();
    }

    public void BeginShutdownSequence()
    {
        _shutdownMode = true;
    }

    public void Update(double deltaSeconds)
    {
        if (!_shutdownMode)
        {
            SpawnAsteroids(deltaSeconds);
        }

        _physicsWorld.Step((float)deltaSeconds);
        UpdateAsteroidsFromPhysics(deltaSeconds);
        UpdateExplosions(deltaSeconds);
        UpdateDebris(deltaSeconds);
    }

    public void OnClick(WpfPoint clickPosition)
    {
        if (_shutdownMode)
        {
            return;
        }

        for (int i = _asteroids.Count - 1; i >= 0; i--)
        {
            AsteroidEntity asteroid = _asteroids[i];
            double hitRadius = Math.Max(asteroid.Image.Width, asteroid.Image.Height) * 0.45;
            if ((asteroid.Position - clickPosition).Length > hitRadius)
            {
                continue;
            }

            asteroid.HitPoints--;
            _asteroidBumpAudioPlayer.Play();
            if (asteroid.HitPoints <= 0)
            {
                DestroyAsteroid(i, asteroid.Position);
            }
            else
            {
                TriggerHitShake(asteroid);
            }

            break;
        }
    }

    public bool IsPointOverAsteroid(WpfPoint point)
    {
        for (int i = _asteroids.Count - 1; i >= 0; i--)
        {
            AsteroidEntity asteroid = _asteroids[i];
            double hitRadius = Math.Max(asteroid.Image.Width, asteroid.Image.Height) * 0.45;
            if ((asteroid.Position - point).Length <= hitRadius)
            {
                return true;
            }
        }

        return false;
    }

    public bool DestroyOneAsteroidForShutdown()
    {
        if (_asteroids.Count == 0)
        {
            return false;
        }

        int index = _random.Next(_asteroids.Count);
        DestroyAsteroid(index, _asteroids[index].Position);
        return true;
    }

    private void SpawnAsteroids(double deltaSeconds)
    {
        _spawnAccumulator += deltaSeconds;
        while (_spawnAccumulator >= _config.SpawnIntervalSeconds && _asteroids.Count < _config.MaxSpawnedAsteroids)
        {
            _spawnAccumulator -= _config.SpawnIntervalSeconds;
            SpawnSingleAsteroid();
        }
    }

    private void SpawnSingleAsteroid()
    {
        Rect spawnBounds = PickSpawnBounds();
        int edge = _random.Next(4);
        double edgeInset = 24;
        WpfPoint spawn = edge switch
        {
            0 => new WpfPoint(RandomRange(spawnBounds.Left + edgeInset, spawnBounds.Right - edgeInset), spawnBounds.Top + edgeInset),
            1 => new WpfPoint(spawnBounds.Right - edgeInset, RandomRange(spawnBounds.Top + edgeInset, spawnBounds.Bottom - edgeInset)),
            2 => new WpfPoint(RandomRange(spawnBounds.Left + edgeInset, spawnBounds.Right - edgeInset), spawnBounds.Bottom - edgeInset),
            _ => new WpfPoint(spawnBounds.Left + edgeInset, RandomRange(spawnBounds.Top + edgeInset, spawnBounds.Bottom - edgeInset))
        };

        WpfVector velocity = CreateSpawnVelocity(spawn, spawnBounds);
        double scale = RandomRange(_config.MinAsteroidScale, _config.MaxAsteroidScale);
        CroppedBitmap sprite = _asteroidFrames[_random.Next(_asteroidFrames.Length)];
        var image = new WpfImage
        {
            Source = sprite,
            RenderTransformOrigin = new WpfPoint(0.5, 0.5),
            Width = sprite.PixelWidth * scale,
            Height = sprite.PixelHeight * scale,
            IsHitTestVisible = false
        };

        var rotation = new RotateTransform(_random.NextDouble() * 360.0);
        image.RenderTransform = rotation;
        _canvas.Children.Add(image);
        Canvas.SetLeft(image, spawn.X - image.Width / 2);
        Canvas.SetTop(image, spawn.Y - image.Height / 2);

        float radiusMeters = (float)((Math.Max(image.Width, image.Height) * 0.45) / _config.PixelsPerMeter);
        var body = BodyFactory.CreateCircle(_physicsWorld, radiusMeters, 1f, ToWorld(spawn));
        body.BodyType = BodyType.Dynamic;
        body.Restitution = 0.9f;
        body.Friction = 0.2f;
        body.LinearDamping = 0.05f;
        body.AngularDamping = 0.05f;
        body.LinearVelocity = ToWorld(velocity);
        body.IgnoreGravity = true;
        body.CollisionCategories = Category.Cat1;
        body.CollidesWith = Category.Cat1;

        _asteroids.Add(new AsteroidEntity(
            image,
            rotation,
            body,
            spawn,
            RandomRange(-95, 95),
            CalculateHitPointsFromScale(scale)));
    }

    private WpfVector CreateSpawnVelocity(WpfPoint spawnPosition, Rect spawnBounds)
    {
        double distanceTop = Math.Abs(spawnPosition.Y - spawnBounds.Top);
        double distanceBottom = Math.Abs(spawnBounds.Bottom - spawnPosition.Y);
        double distanceLeft = Math.Abs(spawnPosition.X - spawnBounds.Left);
        double distanceRight = Math.Abs(spawnBounds.Right - spawnPosition.X);

        double minDistance = distanceTop;
        double baseAngle = Math.PI * 0.5;
        if (distanceBottom < minDistance)
        {
            minDistance = distanceBottom;
            baseAngle = -Math.PI * 0.5;
        }

        if (distanceLeft < minDistance)
        {
            minDistance = distanceLeft;
            baseAngle = 0;
        }

        if (distanceRight < minDistance)
        {
            baseAngle = Math.PI;
        }

        double spread = Math.PI * 0.6;
        double angle = baseAngle + RandomRange(-spread * 0.5, spread * 0.5);
        double speed = RandomRange(_config.MinSpeed, _config.MaxSpeed);
        return new WpfVector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
    }

    private int CalculateHitPointsFromScale(double scale)
    {
        double scaleRange = Math.Max(0.001, _config.MaxAsteroidScale - _config.MinAsteroidScale);
        double normalizedScale = Math.Clamp((scale - _config.MinAsteroidScale) / scaleRange, 0, 1);
        double rawHitPoints = _config.MinClicksToDestroy + ((_config.MaxClicksToDestroy - _config.MinClicksToDestroy) * normalizedScale);
        int variation = _random.Next(-1, 2);
        return Math.Clamp((int)Math.Round(rawHitPoints) + variation, _config.MinClicksToDestroy, _config.MaxClicksToDestroy);
    }

    private void UpdateAsteroidsFromPhysics(double deltaSeconds)
    {
        for (int i = _asteroids.Count - 1; i >= 0; i--)
        {
            AsteroidEntity asteroid = _asteroids[i];
            ApplyCursorInfluence(asteroid, deltaSeconds);
            asteroid.Position = ToScreen(asteroid.Body.Position);
            EnableWallCollisionWhenFullyVisible(asteroid);
            asteroid.Rotation.Angle += asteroid.RotationSpeed * deltaSeconds;
            WpfVector shakeOffset = ComputeShakeOffset(asteroid, deltaSeconds);

            Canvas.SetLeft(asteroid.Image, asteroid.Position.X - asteroid.Image.Width / 2 + shakeOffset.X);
            Canvas.SetTop(asteroid.Image, asteroid.Position.Y - asteroid.Image.Height / 2 + shakeOffset.Y);
        }
    }

    private void ApplyCursorInfluence(AsteroidEntity asteroid, double deltaSeconds)
    {
        WpfVector toAsteroid = asteroid.Position - _cursorPosition;
        double distance = toAsteroid.Length;
        if (distance < 1 || distance > _config.CursorInfluenceRadius)
        {
            return;
        }

        toAsteroid.Normalize();
        double factor = 1.0 - (distance / _config.CursorInfluenceRadius);
        WpfVector impulse = toAsteroid * (_config.CursorPushStrength * factor * deltaSeconds);
        asteroid.Body.ApplyLinearImpulse(ToWorld(impulse));
        CapBodySpeed(asteroid.Body);
    }

    private void UpdateExplosions(double deltaSeconds)
    {
        for (int i = _explosions.Count - 1; i >= 0; i--)
        {
            ExplosionEntity explosion = _explosions[i];
            explosion.Elapsed += deltaSeconds;
            int frameIndex = Math.Min((int)(explosion.Elapsed * _config.ExplosionFrameRate), _explosionFrames.Length - 1);
            explosion.Image.Source = _explosionFrames[frameIndex];

            if (frameIndex >= _explosionFrames.Length - 1)
            {
                _canvas.Children.Remove(explosion.Image);
                _explosions.RemoveAt(i);
            }
        }
    }

    private void DestroyAsteroid(int asteroidIndex, WpfPoint location)
    {
        AsteroidEntity asteroid = _asteroids[asteroidIndex];
        ApplyExplosionImpulse(location, asteroid);
        SpawnDebris(asteroid);
        _physicsWorld.RemoveBody(asteroid.Body);
        _canvas.Children.Remove(asteroid.Image);
        _asteroids.RemoveAt(asteroidIndex);
        DestroyedAsteroids++;

        var explosionImage = new WpfImage
        {
            Width = _config.ExplosionSize,
            Height = _config.ExplosionSize,
            Source = _explosionFrames[0],
            IsHitTestVisible = false
        };
        _canvas.Children.Add(explosionImage);
        Canvas.SetLeft(explosionImage, location.X - explosionImage.Width / 2);
        Canvas.SetTop(explosionImage, location.Y - explosionImage.Height / 2);
        _explosions.Add(new ExplosionEntity(explosionImage, 0));
        _explosionAudioPlayer.Play();
    }

    private void SpawnDebris(AsteroidEntity sourceAsteroid)
    {
        double sourceSize = Math.Max(sourceAsteroid.Image.Width, sourceAsteroid.Image.Height);
        int pieceCount = _random.Next(_config.MinDebrisPieces, _config.MaxDebrisPieces + 1);
        double lifeBase = _config.DebrisLifeSeconds * RandomRange(0.8, 1.2);
        WpfPoint center = sourceAsteroid.Position;

        for (int i = 0; i < pieceCount; i++)
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            double speed = RandomRange(_config.DebrisSpeedMin, _config.DebrisSpeedMax) * Math.Max(0.7, sourceSize / 100.0);
            WpfVector velocity = new(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
            CroppedBitmap sprite = _asteroidFrames[_random.Next(_asteroidFrames.Length)];
            double scale = RandomRange(_config.DebrisScaleMin, _config.DebrisScaleMax) * Math.Max(0.75, sourceSize / 90.0);

            var image = new WpfImage
            {
                Source = sprite,
                Width = Math.Max(4, sprite.PixelWidth * scale),
                Height = Math.Max(4, sprite.PixelHeight * scale),
                Opacity = RandomRange(0.75, 1.0),
                IsHitTestVisible = false,
                RenderTransformOrigin = new WpfPoint(0.5, 0.5)
            };
            _canvas.Children.Add(image);

            var debris = new DebrisEntity(
                image,
                center,
                velocity,
                lifeBase * RandomRange(0.85, 1.15),
                RandomRange(-420, 420),
                _random.NextDouble() * 360);
            _debris.Add(debris);
        }
    }

    private void ApplyExplosionImpulse(WpfPoint center, AsteroidEntity sourceAsteroid)
    {
        double sourceSize = Math.Max(sourceAsteroid.Image.Width, sourceAsteroid.Image.Height);
        double radiusPixels = sourceSize * _config.ExplosionRadiusMultiplier;
        double forcePixels = sourceSize * _config.ExplosionForceMultiplier;
        if (radiusPixels <= 1 || forcePixels <= 0.01)
        {
            return;
        }

        for (int i = 0; i < _asteroids.Count; i++)
        {
            AsteroidEntity asteroid = _asteroids[i];
            if (ReferenceEquals(asteroid, sourceAsteroid))
            {
                continue;
            }

            WpfVector delta = asteroid.Position - center;
            double distance = delta.Length;
            if (distance >= radiusPixels)
            {
                continue;
            }

            if (distance < 0.001)
            {
                delta = new WpfVector(RandomRange(-1, 1), RandomRange(-1, 1));
                if (delta.Length < 0.001)
                {
                    delta = new WpfVector(1, 0);
                }

                distance = delta.Length;
            }

            delta.Normalize();
            double falloff = 1.0 - (distance / radiusPixels);
            double impulsePixels = forcePixels * falloff;
            asteroid.Body.ApplyLinearImpulse(ToWorld(delta * impulsePixels));
            CapBodySpeed(asteroid.Body);
        }
    }

    private double GetWorldWidth() => Math.Max(_canvas.ActualWidth, _config.MinWorldWidth);
    private double GetWorldHeight() => Math.Max(_canvas.ActualHeight, _config.MinWorldHeight);
    private Rect GetFallbackBounds() => new(0, 0, GetWorldWidth(), GetWorldHeight());
    private double RandomRange(double min, double max) => min + _random.NextDouble() * (max - min);
    private Microsoft.Xna.Framework.Vector2 ToWorld(WpfPoint point) => new((float)(point.X / _config.PixelsPerMeter), (float)(point.Y / _config.PixelsPerMeter));
    private Microsoft.Xna.Framework.Vector2 ToWorld(WpfVector vector) => new((float)(vector.X / _config.PixelsPerMeter), (float)(vector.Y / _config.PixelsPerMeter));
    private WpfPoint ToScreen(Microsoft.Xna.Framework.Vector2 value) => new(value.X * _config.PixelsPerMeter, value.Y * _config.PixelsPerMeter);

    private void CapBodySpeed(Body body)
    {
        float max = (float)(_config.MaxSpeedLimit / _config.PixelsPerMeter);
        if (body.LinearVelocity.LengthSquared() <= max * max)
        {
            return;
        }

        Microsoft.Xna.Framework.Vector2 velocity = Microsoft.Xna.Framework.Vector2.Normalize(body.LinearVelocity) * max;
        body.LinearVelocity = velocity;
    }

    private Rect PickSpawnBounds()
    {
        if (_monitorBounds.Count == 0)
        {
            return GetFallbackBounds();
        }

        return _monitorBounds[_random.Next(_monitorBounds.Count)];
    }

    private void EnableWallCollisionWhenFullyVisible(AsteroidEntity asteroid)
    {
        if (asteroid.HasWallCollision)
        {
            return;
        }

        double radius = Math.Max(asteroid.Image.Width, asteroid.Image.Height) * 0.5;
        IReadOnlyList<Rect> bounds = _monitorBounds.Count > 0 ? _monitorBounds : new[] { GetFallbackBounds() };
        for (int i = 0; i < bounds.Count; i++)
        {
            Rect area = bounds[i];
            if (asteroid.Position.X - radius >= area.Left &&
                asteroid.Position.X + radius <= area.Right &&
                asteroid.Position.Y - radius >= area.Top &&
                asteroid.Position.Y + radius <= area.Bottom)
            {
                asteroid.HasWallCollision = true;
                asteroid.Body.CollidesWith = Category.All;
                return;
            }
        }
    }

    private void TriggerHitShake(AsteroidEntity asteroid)
    {
        double size = Math.Max(asteroid.Image.Width, asteroid.Image.Height);
        asteroid.ShakeDuration = _config.HitShakeDuration;
        asteroid.ShakeTimeRemaining = _config.HitShakeDuration;
        asteroid.ShakeMagnitude = Math.Max(2.5, size * _config.HitShakeMagnitudeMultiplier);
        asteroid.ShakeFrequency = _config.HitShakeFrequency;
        asteroid.ShakePhase = RandomRange(0, Math.PI * 2);
    }

    private WpfVector ComputeShakeOffset(AsteroidEntity asteroid, double deltaSeconds)
    {
        if (asteroid.ShakeTimeRemaining <= 0 || asteroid.ShakeDuration <= 0)
        {
            return new WpfVector(0, 0);
        }

        asteroid.ShakeTimeRemaining = Math.Max(0, asteroid.ShakeTimeRemaining - deltaSeconds);
        double normalizedTime = asteroid.ShakeTimeRemaining / asteroid.ShakeDuration;
        double amplitude = asteroid.ShakeMagnitude * normalizedTime;
        double t = asteroid.ShakeTimeRemaining * asteroid.ShakeFrequency;
        double x = Math.Sin((t * 1.8) + asteroid.ShakePhase) * amplitude;
        double y = Math.Cos((t * 2.1) + asteroid.ShakePhase * 0.7) * amplitude * 0.85;
        return new WpfVector(x, y);
    }

    private void UpdateDebris(double deltaSeconds)
    {
        for (int i = _debris.Count - 1; i >= 0; i--)
        {
            DebrisEntity debris = _debris[i];
            debris.RemainingLife -= deltaSeconds;
            if (debris.RemainingLife <= 0)
            {
                _canvas.Children.Remove(debris.Image);
                _debris.RemoveAt(i);
                continue;
            }

            double drag = Math.Exp(-_config.DebrisDragPerSecond * deltaSeconds);
            debris.Velocity *= drag;
            debris.Position += debris.Velocity * deltaSeconds;
            debris.RotationAngle += debris.RotationSpeed * deltaSeconds;

            double lifeRatio = debris.RemainingLife / Math.Max(0.001, debris.LifeTime);
            debris.Image.Opacity = Math.Clamp(lifeRatio, 0, 1);
            debris.Image.RenderTransform = new RotateTransform(debris.RotationAngle);
            Canvas.SetLeft(debris.Image, debris.Position.X - debris.Image.Width / 2);
            Canvas.SetTop(debris.Image, debris.Position.Y - debris.Image.Height / 2);
        }
    }

    private void RebuildMonitorWalls()
    {
        foreach (Body wallBody in _monitorWallBodies)
        {
            _physicsWorld.RemoveBody(wallBody);
        }

        _monitorWallBodies.Clear();
        IReadOnlyList<Rect> targetBounds = _monitorBounds.Count > 0 ? _monitorBounds : new[] { GetFallbackBounds() };
        const float wallThickness = 0.3f;
        const double edgeTolerance = 0.5;

        foreach (Rect bounds in targetBounds.Where(static b => b.Width > 1 && b.Height > 1))
        {
            float width = (float)(bounds.Width / _config.PixelsPerMeter);
            float height = (float)(bounds.Height / _config.PixelsPerMeter);
            float left = (float)(bounds.Left / _config.PixelsPerMeter);
            float top = (float)(bounds.Top / _config.PixelsPerMeter);
            float centerX = left + (width * 0.5f);
            float centerY = top + (height * 0.5f);

            bool hasTopNeighbor = HasNeighborOnHorizontalEdge(targetBounds, bounds.Top, bounds.Left, bounds.Right, isTopEdge: true, edgeTolerance);
            bool hasBottomNeighbor = HasNeighborOnHorizontalEdge(targetBounds, bounds.Bottom, bounds.Left, bounds.Right, isTopEdge: false, edgeTolerance);
            bool hasLeftNeighbor = HasNeighborOnVerticalEdge(targetBounds, bounds.Left, bounds.Top, bounds.Bottom, isLeftEdge: true, edgeTolerance);
            bool hasRightNeighbor = HasNeighborOnVerticalEdge(targetBounds, bounds.Right, bounds.Top, bounds.Bottom, isLeftEdge: false, edgeTolerance);

            if (!hasTopNeighbor)
            {
                _monitorWallBodies.Add(CreateWallBody(centerX, top - wallThickness, width * 0.5f, wallThickness));
            }

            if (!hasBottomNeighbor)
            {
                _monitorWallBodies.Add(CreateWallBody(centerX, top + height + wallThickness, width * 0.5f, wallThickness));
            }

            if (!hasLeftNeighbor)
            {
                _monitorWallBodies.Add(CreateWallBody(left - wallThickness, centerY, wallThickness, height * 0.5f));
            }

            if (!hasRightNeighbor)
            {
                _monitorWallBodies.Add(CreateWallBody(left + width + wallThickness, centerY, wallThickness, height * 0.5f));
            }
        }
    }

    private static bool HasNeighborOnHorizontalEdge(IReadOnlyList<Rect> allBounds, double edgeY, double xStart, double xEnd, bool isTopEdge, double tolerance)
    {
        for (int i = 0; i < allBounds.Count; i++)
        {
            Rect other = allBounds[i];
            if (other.Width <= 1 || other.Height <= 1)
            {
                continue;
            }

            double neighborEdge = isTopEdge ? other.Bottom : other.Top;
            if (Math.Abs(neighborEdge - edgeY) > tolerance)
            {
                continue;
            }

            double overlap = Math.Min(xEnd, other.Right) - Math.Max(xStart, other.Left);
            if (overlap > tolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNeighborOnVerticalEdge(IReadOnlyList<Rect> allBounds, double edgeX, double yStart, double yEnd, bool isLeftEdge, double tolerance)
    {
        for (int i = 0; i < allBounds.Count; i++)
        {
            Rect other = allBounds[i];
            if (other.Width <= 1 || other.Height <= 1)
            {
                continue;
            }

            double neighborEdge = isLeftEdge ? other.Right : other.Left;
            if (Math.Abs(neighborEdge - edgeX) > tolerance)
            {
                continue;
            }

            double overlap = Math.Min(yEnd, other.Bottom) - Math.Max(yStart, other.Top);
            if (overlap > tolerance)
            {
                return true;
            }
        }

        return false;
    }

    private Body CreateWallBody(float centerX, float centerY, float halfWidth, float halfHeight)
    {
        Body body = BodyFactory.CreateBody(_physicsWorld, new Microsoft.Xna.Framework.Vector2(centerX, centerY), 0, BodyType.Static);
        FixtureFactory.AttachRectangle(halfWidth * 2f, halfHeight * 2f, 1f, Microsoft.Xna.Framework.Vector2.Zero, body);
        body.CollisionCategories = Category.Cat2;
        body.CollidesWith = Category.All;
        return body;
    }

    public void Dispose()
    {
        for (int i = _debris.Count - 1; i >= 0; i--)
        {
            _canvas.Children.Remove(_debris[i].Image);
        }

        _debris.Clear();
        _explosionAudioPlayer.Dispose();
        _asteroidBumpAudioPlayer.Dispose();
    }
}
