using ScanAlign.App.Viewport;

namespace ScanAlign.Tests.App;

public class OrbitCameraTests
{
    [Fact]
    public void Position_is_target_plus_direction_times_distance()
    {
        var cam = new OrbitCamera { Target = new Vector3(1, 2, 3), Distance = 10f };
        var expected = cam.Target + (cam.Direction * 10f);
        Assert.True(Vector3.Distance(cam.Position, expected) < 1e-4f);
        Assert.True(Vector3.Dot(cam.LookDirection, cam.Direction) < 0); // look points toward target
    }

    [Fact]
    public void Pitch_is_clamped_below_vertical()
    {
        var cam = new OrbitCamera();
        cam.Orbit(0, 100f);
        Assert.True(cam.Pitch < 1.5708f); // never reaches 90°
        cam.Orbit(0, -200f);
        Assert.True(cam.Pitch > -1.5708f);
    }

    [Fact]
    public void Zoom_scales_distance_and_stays_positive()
    {
        var cam = new OrbitCamera { Distance = 8f };
        cam.Zoom(0.5f);
        Assert.Equal(4f, cam.Distance, 4);
        cam.Zoom(0f); // would be zero, but distance is clamped above zero
        Assert.True(cam.Distance > 0);
    }

    [Fact]
    public void FrameExtents_centers_and_backs_off()
    {
        var cam = new OrbitCamera();
        cam.FrameExtents(new Vector3(5, 5, 5), radius: 4f);
        Assert.Equal(new Vector3(5, 5, 5), cam.Target);
        Assert.True(cam.Distance >= 4f);
    }
}
