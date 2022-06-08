// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework
{
    public class AudioLatencyTester : CompositeDrawable
    {
        [Resolved]
        private AudioManager audio { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        public AudioLatencyTester()
        {
            RelativeSizeAxes = Axes.Both;
        }

        private readonly BindableDouble audioRateAdjust = new BindableDouble(1)
        {
            MinValue = 0f,
            MaxValue = 1f,
        };

        protected override void LoadComplete()
        {
            var track = audio.Tracks.Get("tick-track.mp3");
            var waveform = new Waveform(audio.Tracks.GetStream("tick-track.mp3"));

            audio.AddAdjustment(AdjustableProperty.Frequency, audioRateAdjust);

            var rateAdjustClock = new StopwatchClock(true);
            var framedClock = new FramedClock(rateAdjustClock);

            audioRateAdjust.BindValueChanged(e => rateAdjustClock.Rate = e.NewValue, true);

            AddRangeInternal(new Drawable[]
            {
                new DrawableTrack(track),
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.X,
                    Height = 200,
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Y = -315,
                            Text = "Playback rate",
                        },
                        new BasicSliderBar<double>
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Y = -200,
                            Size = new Vector2(300, 50),
                            Current = audioRateAdjust,
                        },
                        new SpriteText
                        {
                            Y = -20,
                            Text = "Track",
                        },
                        new Box
                        {
                            Colour = FrameworkColour.GreenDarker,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.X,
                            Height = 1f,
                            Colour = FrameworkColour.Yellow,
                        },
                        new WaveformGraph
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Waveform = waveform,
                            BaseColour = FrameworkColour.Yellow,
                            MidColour = FrameworkColour.YellowDark.Darken(0.25f),
                            LowColour = FrameworkColour.YellowDark,
                            HighColour = FrameworkColour.Yellow.Lighten(0.25f),
                        },
                        new SamplePlayer(track)
                        {
                            Clock = framedClock,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Y = 130,
                            Text = "Music volume",
                        },
                        new BasicSliderBar<double>
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Y = 100,
                            Size = new Vector2(300, 50),
                            Current = config.GetBindable<double>(FrameworkSetting.VolumeMusic),
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Y = 230,
                            Text = "Sample volume",
                        },
                        new BasicSliderBar<double>
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Y = 200,
                            Size = new Vector2(300, 50),
                            Current = config.GetBindable<double>(FrameworkSetting.VolumeEffect),
                        },
                    }
                },
            });

            track.Looping = true;
            track.Restart();
        }

        private class SamplePlayer : CompositeDrawable
        {
            private readonly Track track;

            private Box marker;

            private Sample sample;

            public SamplePlayer(Track track)
            {
                this.track = track;

                RelativeSizeAxes = Axes.Both;
            }

            private double lastTrackTime;
            private double nextBeat;

            [BackgroundDependencyLoader]
            private void load(AudioManager audio)
            {
                sample = audio.Samples.Get("tick-sample.mp3");

                AddInternal(marker = new Box
                {
                    RelativePositionAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Width = 3f,
                    Colour = FrameworkColour.Blue,
                });
            }

            protected override void Update()
            {
                base.Update();

                if (track.IsRunning)
                {
                    marker.Alpha = 1;
                    marker.X = (float)(track.CurrentTime / track.Length);
                }
                else
                    marker.Alpha = 0;

                if (lastTrackTime > track.CurrentTime || nextBeat == 0)
                    nextBeat = 200;

                if (track.CurrentTime >= nextBeat)
                {
                    sample.Play();

                    Drawable afterimage;

                    AddInternal(afterimage = new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativePositionAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        X = marker.X,
                        Width = marker.Width,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = marker.Colour,
                                RelativeSizeAxes = Axes.Both
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Y = -20,
                                Text = "Sample",
                            },
                        }
                    });

                    afterimage.ScaleTo(2, 400, Easing.OutQuint)
                              .FadeOut(400, Easing.InQuint)
                              .Expire();

                    nextBeat = double.PositiveInfinity;
                }

                lastTrackTime = track.CurrentTime;
            }
        }
    }
}
