using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NavigationAnimationForPrism.Avalonia;

/// <summary>
/// Displays <see cref="ContentControl.Content"/> according to an <see cref="IDataTemplate"/>,
/// </summary>
public class InOutContentControl : ContentControl
{
    private ContentPresenter? _oldPresenter;
    private bool              _shouldAnimate;

    #region TransitionCompletedEvent

    /// <summary>
    /// Defines the <see cref="TransitionCompleted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<TransitionCompletedEventArgs> TransitionCompletedEvent =
        RoutedEvent.Register<InOutContentControl, TransitionCompletedEventArgs>(
                                                                                nameof(TransitionCompleted),
                                                                                RoutingStrategies.Direct);


    /// <summary>
    /// Raised when the old content isn't needed anymore by the control, because the transition has completed.
    /// </summary>
    public event EventHandler<TransitionCompletedEventArgs> TransitionCompleted
    {
        add => AddHandler(TransitionCompletedEvent, value);
        remove => RemoveHandler(TransitionCompletedEvent, value);
    }

    #endregion

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        if (_shouldAnimate)
        {
            Debug.Print("ArrangeOverride 开始执行动画.");

            if (_oldPresenter is not null &&
                Presenter is not null)
            {
                _shouldAnimate = false;

                var tasks = new List<Task>();


                if (_oldPresenter.Content is IInOutAnimation f)
                {
                    Debug.Print("oldContent 开始 Out 动画.");

                    //如果当前正在进行动画, 那么取消
                    f.OutCancellationTokenSource?.Cancel();
                    f.InCancellationTokenSource?.Cancel();


                    var outTask = f.RunOutAnimationAsync();

                    if (outTask is not null)
                    {
                        tasks.Add(outTask);
                    }
                }
                else
                {
                    Debug.Print("oldContent 没有 Out 动画,直接隐藏.");
                    HideOldPresenter();
                }


                if (Content is IInOutAnimation f2)
                {
                    Debug.Print("newContent 开始 In 动画.");

                    f2.OutCancellationTokenSource?.Cancel();
                    f2.InCancellationTokenSource?.Cancel();

                    var inTask = f2.RunInAnimationAsync();

                    if (inTask is not null)
                    {
                        tasks.Add(inTask);
                    }
                }

                //out 和 in 动画全部完成后, 激活 TransitionCompleted 事件
                if (tasks.Any())
                {
                    Task.WhenAll(tasks)
                        .ContinueWith(task =>
                                      {
                                          OnTransitionCompleted(new TransitionCompletedEventArgs(
                                                                                                 _oldPresenter.Content,
                                                                                                 Content,
                                                                                                 task.Status == TaskStatus.RanToCompletion
                                                                                                )
                                                               );
                                      }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }

            _shouldAnimate = false;
        }

        return result;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateContent(false);
    }

    protected override bool RegisterContentPresenter(ContentPresenter presenter)
    {
        if (base.RegisterContentPresenter(presenter)) //在基类中处理了 PART_ContentPresenter
        {
            return true;
        }

        if (presenter is { Name: "PART_ContentPresenter2" })
        {
            _oldPresenter           = presenter;
            _oldPresenter.IsVisible = false;
            UpdateContent(false);
            return true;
        }

        return false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ContentProperty)
        {
            UpdateContent(true);
            return;
        }

        base.OnPropertyChanged(change);
    }

    private void UpdateContent(bool withTransition)
    {
        if (VisualRoot is null || _oldPresenter is null || Presenter is null)
        {
            return;
        }

        //原本的内容放入 _presenter2 中
        var oldContent = Presenter.Content;
        Presenter.Content = null;
        if (oldContent is not null)
        {
            if (_oldPresenter.Content is IInOutAnimation o)
            {
                o.OutAnimationCompleted -= OldPresenter_OutAnimationCompleted;
            }

            _oldPresenter.Content = oldContent;
            if (_oldPresenter.Content is IInOutAnimation o2)
            {
                o2.OutAnimationCompleted += OldPresenter_OutAnimationCompleted;
            }


            _oldPresenter.IsVisible = true;
        }

        //新的内容放入 Presenter 中
        var newContent = Content;
        Presenter.Content   = newContent;
        Presenter.IsVisible = true;


        if (withTransition) //无论新内容是否为null,老的内容都需要退出
        {
            _shouldAnimate = true;
            InvalidateArrange();
        }
        else
        {
            HideOldPresenter();
            OnTransitionCompleted(new TransitionCompletedEventArgs(oldContent, newContent, false));
        }
    }

    private void OldPresenter_OutAnimationCompleted(object? sender, AnimationCompletedEventArgs e)
    {
        //当动画完成后且没有新的动画在播放,那么隐藏HideOldPresenter
        if (!e.ANewAnimationIsRunning)
        {
            Dispatcher.UIThread.Invoke(HideOldPresenter);
        }
    }

    private void HideOldPresenter()
    {
        if (_oldPresenter is not null)
        {
            _oldPresenter.IsVisible = false;
            _oldPresenter.Content   = null;
        }
    }

    private void OnTransitionCompleted(TransitionCompletedEventArgs e)
        => RaiseEvent(e);
}
