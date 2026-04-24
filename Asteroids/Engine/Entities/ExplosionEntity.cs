using System.Windows.Controls;
using WpfImage = System.Windows.Controls.Image;

namespace Asteroids.Engine.Entities;

public sealed class ExplosionEntity
{
    public ExplosionEntity(WpfImage image, double elapsed)
    {
        Image = image;
        Elapsed = elapsed;
    }

    public WpfImage Image { get; }
    public double Elapsed { get; set; }
}
