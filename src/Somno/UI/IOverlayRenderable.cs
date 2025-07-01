namespace Somno.UI;

/// <summary>
/// Represents an object that is capable of rendering to a screen overlay.
/// </summary>
internal interface IOverlayRenderable
{
    /// <summary>
    /// Handles drawing to the calling overlay.
    /// </summary>
    /// <param name="overlay">The calling overlay.</param>
    public void RenderOnOverlay(SomnoOverlay overlay);

    /// <summary>
    /// Whether the object depends on the game running in order to
    /// draw on the overlay.
    /// </summary>
    public bool OverlayRenderDependsOnGame { get; }

    /// <summary>
    /// Called when the overlay is being destroyed.
    /// </summary>
    public void OnOverlayDestroy() { }
}
