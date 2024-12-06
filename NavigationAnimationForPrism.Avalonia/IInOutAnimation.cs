using System;
using System.Threading;
using System.Threading.Tasks;

namespace NavigationAnimationForPrism.Avalonia;

public interface IInOutAnimation
{
    public CancellationTokenSource?                         InCancellationTokenSource { get; }
    public Task?                                            RunInAnimationAsync();
    public event EventHandler<AnimationCompletedEventArgs>? InAnimationCompleted;

    public CancellationTokenSource?                         OutCancellationTokenSource { get; }
    public Task?                                            RunOutAnimationAsync();
    public event EventHandler<AnimationCompletedEventArgs>? OutAnimationCompleted;
}

public class AnimationCompletedEventArgs : EventArgs
{
    public bool IsCanceled             { get; set; }
    public bool ANewAnimationIsRunning { get; set; }
}
