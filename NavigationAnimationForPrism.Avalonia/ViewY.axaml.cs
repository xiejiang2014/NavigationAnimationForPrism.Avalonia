using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace NavigationAnimationForPrism.Avalonia;

public partial class ViewY : UserControl, IInOutAnimation
{
    public ViewY()
    {
        InitializeComponent();
    }

    private readonly Dictionary<Task, CancellationTokenSource> _tc = new();

    public bool                                             IsInAnimationRunning      { get; private set; }
    public CancellationTokenSource?                         InCancellationTokenSource { get; private set; }
    public event EventHandler<AnimationCompletedEventArgs>? InAnimationCompleted;

    private Animation InAnimation => new()
                                     {
                                         Children =
                                         {
                                             new KeyFrame()
                                             {
                                                 Setters =
                                                 {
                                                     new Setter
                                                     {
                                                         Property = OpacityProperty,
                                                         Value = _inAniId <= 1 && _outAniId <= 1
                                                             ? 0
                                                             : Opacity
                                                     },
                                                     new Setter
                                                     {
                                                         Property = ScaleTransform.ScaleXProperty,
                                                         Value    = this.RenderTransform?.Value.M11 ?? 6d,
                                                     },
                                                     new Setter
                                                     {
                                                         Property = ScaleTransform.ScaleYProperty,
                                                         Value    = this.RenderTransform?.Value.M22 ?? 6d,
                                                     },
                                                 },
                                                 Cue = new Cue(0d)
                                             },
                                             new KeyFrame()
                                             {
                                                 Setters =
                                                 {
                                                     new Setter
                                                     {
                                                         Property = OpacityProperty,
                                                         Value    = 1d
                                                     },
                                                     new Setter
                                                     {
                                                         Property = ScaleTransform.ScaleXProperty,
                                                         Value    = 1d
                                                     },
                                                     new Setter
                                                     {
                                                         Property = ScaleTransform.ScaleYProperty,
                                                         Value    = 1d
                                                     },
                                                 },
                                                 Cue = new Cue(1d)
                                             }
                                         },
                                         FillMode = FillMode.Forward,
                                         Duration = TimeSpan.FromMilliseconds(400)
                                     };


    private int _inAniId;

    public Task RunInAnimationAsync()
    {
        _inAniId++;

        var inAniId = _inAniId;
        InCancellationTokenSource = new CancellationTokenSource();

        IsInAnimationRunning = true;
        Debug.Print($"{GetType().Name} out 动画开始  Opacity:{Opacity}");
        var task = InAnimation.RunAsync(this, InCancellationTokenSource.Token);
        _tc.Add(task, InCancellationTokenSource);
        task.ContinueWith(t =>
                          {
                              var c = _tc[t];

                              if (c.IsCancellationRequested) //动画被中断
                              {
                                  if (inAniId == _inAniId) //相等表示没有新的动画实例被运行
                                  {
                                      IsInAnimationRunning = false;
                                  }
                              }
                              else
                              {
                              }

                              InAnimationCompleted?.Invoke(this,
                                                           new AnimationCompletedEventArgs()
                                                           {
                                                               IsCanceled             = c.IsCancellationRequested,
                                                               ANewAnimationIsRunning = inAniId != _inAniId
                                                           }
                                                          );

                              Debug.Print($"{GetType().Name} out 动画结束");
                              _tc.Remove(t);
                          });


        return task;
    }

    public bool                                             IsOutAnimationRunning      { get; private set; }
    public CancellationTokenSource?                         OutCancellationTokenSource { get; private set; }
    public event EventHandler<AnimationCompletedEventArgs>? OutAnimationCompleted;

    private Animation OutAnimation => new()
                                      {
                                          Children =
                                          {
                                              new KeyFrame()
                                              {
                                                  Setters =
                                                  {
                                                      new Setter
                                                      {
                                                          Property = OpacityProperty,
                                                          Value = _inAniId <= 1 && _outAniId <= 1
                                                              ? 1
                                                              : Opacity
                                                      },
                                                      new Setter
                                                      {
                                                          Property = ScaleTransform.ScaleXProperty,
                                                          Value    = RenderTransform?.Value.M11 ?? 1d,
                                                      },
                                                      new Setter
                                                      {
                                                          Property = ScaleTransform.ScaleYProperty,
                                                          Value    = RenderTransform?.Value.M22 ?? 1d,
                                                      },
                                                  },
                                                  Cue = new Cue(0d)
                                              },
                                              new KeyFrame()
                                              {
                                                  Setters =
                                                  {
                                                      new Setter
                                                      {
                                                          Property = OpacityProperty,
                                                          Value    = 0d
                                                      },
                                                      new Setter
                                                      {
                                                          Property = ScaleTransform.ScaleXProperty,
                                                          Value    = 6d
                                                      },
                                                      new Setter
                                                      {
                                                          Property = ScaleTransform.ScaleYProperty,
                                                          Value    = 6d
                                                      },
                                                  },
                                                  Cue = new Cue(1d)
                                              }
                                          },
                                          FillMode = FillMode.Forward,
                                          Duration = TimeSpan.FromMilliseconds(400)
                                      };

    private int _outAniId;

    public Task RunOutAnimationAsync()
    {
        _outAniId++;

        var outAniId = _outAniId;

        OutCancellationTokenSource = new CancellationTokenSource();

        IsOutAnimationRunning = true;
        Debug.Print($"{GetType().Name} out 动画开始  Opacity:{Opacity}");
        var task = OutAnimation.RunAsync(this, OutCancellationTokenSource.Token);
        _tc.Add(task, OutCancellationTokenSource);
        task.ContinueWith(t =>
                          {
                              var c = _tc[t];

                              if (c.IsCancellationRequested) //动画被中断
                              {
                                  if (outAniId == _outAniId) //相等表示没有新的动画实例被运行
                                  {
                                      IsOutAnimationRunning = false;
                                  }
                              }

                              OutAnimationCompleted?.Invoke(this,
                                                            new AnimationCompletedEventArgs()
                                                            {
                                                                IsCanceled             = c.IsCancellationRequested,
                                                                ANewAnimationIsRunning = outAniId != _outAniId
                                                            }
                                                           );

                              Debug.Print($"{GetType().Name} out 动画结束");
                              _tc.Remove(t);
                          }
                         );


        return task;
    }
}
