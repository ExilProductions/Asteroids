namespace Asteroids.Engine;

public sealed class AsteroidsGameConfig
{
    public int AsteroidColumns { get; init; } = 7;
    public int AsteroidRows { get; init; } = 7;
    public int ExplosionColumns { get; init; } = 9;
    public int ExplosionRows { get; init; } = 9;
    public double SpawnIntervalSeconds { get; init; } = 0.65;
    public int MaxSpawnedAsteroids { get; init; } = 24;
    public double CursorInfluenceRadius { get; init; } = 180.0;
    public double CursorPushStrength { get; init; } = 400.0;
    public int MinClicksToDestroy { get; init; } = 3;
    public int MaxClicksToDestroy { get; init; } = 6;
    public double MinAsteroidScale { get; init; } = 0.6;
    public double MaxAsteroidScale { get; init; } = 1.5;
    public double MinSpeed { get; init; } = 55.0;
    public double MaxSpeed { get; init; } = 160.0;
    public double MaxSpeedLimit { get; init; } = 240.0;
    public double SpawnPadding { get; init; } = 90.0;
    public double WrapMargin { get; init; } = 140.0;
    public double ExplosionFrameRate { get; init; } = 28.0;
    public double ExplosionSize { get; init; } = 96.0;
    public double ExplosionVolume { get; init; } = 0.55;
    public double AsteroidBumpVolume { get; init; } = 0.4;
    public double ExplosionForceMultiplier { get; init; } = 2.8;
    public double ExplosionRadiusMultiplier { get; init; } = 2.6;
    public double HitShakeDuration { get; init; } = 0.22;
    public double HitShakeMagnitudeMultiplier { get; init; } = 0.09;
    public double HitShakeFrequency { get; init; } = 34.0;
    public int MinDebrisPieces { get; init; } = 6;
    public int MaxDebrisPieces { get; init; } = 14;
    public double DebrisLifeSeconds { get; init; } = 1.4;
    public double DebrisSpeedMin { get; init; } = 55.0;
    public double DebrisSpeedMax { get; init; } = 210.0;
    public double DebrisDragPerSecond { get; init; } = 1.8;
    public double DebrisScaleMin { get; init; } = 0.12;
    public double DebrisScaleMax { get; init; } = 0.32;
    public int MinWorldWidth { get; init; } = 300;
    public int MinWorldHeight { get; init; } = 200;
    public float PixelsPerMeter { get; init; } = 64f;
}
