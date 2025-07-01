namespace Somno.UI;

/// <summary>
/// Represents an object that is capable of displaying ImGUI widgets
/// in designated abstract configuration groups.
/// </summary>
internal interface IConfigRenderable
{
    /// <summary>
    /// Handles drawing configuration options to the calling overlay.
    /// </summary>
    /// <param name="overlay">The calling overlay.</param>
    public void RenderConfiguration(SomnoOverlay overlay);
}
