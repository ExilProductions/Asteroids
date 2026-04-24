using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VelcroPhysics.Dynamics;
using WpfImage = System.Windows.Controls.Image;
using WpfPoint = System.Windows.Point;

namespace Asteroids.Engine.Entities;

public sealed class AsteroidEntity
{
    public AsteroidEntity(
        WpfImage image,
        RotateTransform rotation,
        Body body,
        WpfPoint position,
        double rotationSpeed,
        int hitPoints)
    {
        Image = image;
        Rotation = rotation;
        Body = body;
        Position = position;
        RotationSpeed = rotationSpeed;
        HitPoints = hitPoints;
    }

    public WpfImage Image { get; }
    public RotateTransform Rotation { get; }
    public Body Body { get; }
    public WpfPoint Position { get; set; }
    public double RotationSpeed { get; }
    public int HitPoints { get; set; }
    public bool HasWallCollision { get; set; }
    public double ShakeTimeRemaining { get; set; }
    public double ShakeDuration { get; set; }
    public double ShakeMagnitude { get; set; }
    public double ShakeFrequency { get; set; }
    public double ShakePhase { get; set; }

    public void WrapWithinBounds(double width, double height, double margin)
    {
        if (Position.X < -margin) Position = new WpfPoint(width + margin, Position.Y);
        if (Position.X > width + margin) Position = new WpfPoint(-margin, Position.Y);
        if (Position.Y < -margin) Position = new WpfPoint(Position.X, height + margin);
        if (Position.Y > height + margin) Position = new WpfPoint(Position.X, -margin);
    }
}
