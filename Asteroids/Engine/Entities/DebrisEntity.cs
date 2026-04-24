using System.Windows.Controls;
using WpfImage = System.Windows.Controls.Image;
using WpfPoint = System.Windows.Point;
using WpfVector = System.Windows.Vector;

namespace Asteroids.Engine.Entities;

public sealed class DebrisEntity
{
    public DebrisEntity(WpfImage image, WpfPoint position, WpfVector velocity, double lifeTime, double rotationSpeed, double rotationAngle)
    {
        Image = image;
        Position = position;
        Velocity = velocity;
        LifeTime = lifeTime;
        RemainingLife = lifeTime;
        RotationSpeed = rotationSpeed;
        RotationAngle = rotationAngle;
    }

    public WpfImage Image { get; }
    public WpfPoint Position { get; set; }
    public WpfVector Velocity { get; set; }
    public double LifeTime { get; }
    public double RemainingLife { get; set; }
    public double RotationSpeed { get; }
    public double RotationAngle { get; set; }
}
