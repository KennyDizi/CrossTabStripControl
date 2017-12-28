using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using FFImageLoading.Forms;
using ReactiveUI;
using Xamarin.Forms;

namespace CrossTapStripControl
{
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// CrossTabStripControl
    /// </summary>
    public class CrossTabStripControl : ContentView, IDisposable
    {
        #region Private Members

        private readonly CompositeDisposable _compositeDisposable;

        /// <summary>
        /// The in transition.
        /// </summary>
        private bool _inTransition;

        /// <summary>
        /// The list of views that can be displayed.
        /// </summary>
        private readonly ObservableCollection<TabItem> _children = new ObservableCollection<TabItem>();

        /// <summary>
        /// The tab control.
        /// </summary>
        private readonly ContentView _tabControl;

        /// <summary>
        /// The content view.
        /// </summary>
        private readonly Grid _contentView;

        /// <summary>
        /// The button stack.
        /// </summary>
        private readonly Grid _buttonStack;

        /// <summary>
        /// The indicator.
        /// </summary>
        private readonly TabBarIndicator _indicator;

        public int TabActiveIndex
        {
            get => (int) GetValue(TabActiveIndexProperty);
            set => SetValue(TabActiveIndexProperty, value);
        }

        public static readonly BindableProperty TabActiveIndexProperty = BindableProperty.Create(nameof(TabActiveIndex),
            typeof(int), typeof(CrossTabStripControl), defaultValue: -1, defaultBindingMode: BindingMode.TwoWay);

        #endregion

        #region Events

        /// <summary>
        /// Occurs when tab activated.
        /// </summary>
        public event EventHandler<EventArgs> TabActivated;

        private void RaiseTabActivated(int tabIndex)
        {
            TabActiveIndex = tabIndex;
            TabActivated?.Invoke(this, new EventArgs());
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossTabStripControl"/> class.
        /// </summary>
        public CrossTabStripControl()
        {
            _compositeDisposable = new CompositeDisposable();
            var mainLayout = new RelativeLayout();

            // Create tab control
            _buttonStack = new Grid {ColumnDefinitions = new ColumnDefinitionCollection()};

            //TabBarIndicator
            _indicator = new TabBarIndicator
            {
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Start,
                BackgroundColor = Color.Accent,
                HeightRequest = 3
            };

            //tab control
            _tabControl = new ContentView
            {
                BackgroundColor = TabBackColor,
                Content = new Grid
                {
                    Children =
                    {
                        _buttonStack,
                        _indicator
                    }
                }
            };

            mainLayout.Children.Add(_tabControl, () => new Rectangle(0, 0, mainLayout.Width, TabHeight));

            //create content control
            _contentView = new Grid();

            mainLayout.Children.Add(_contentView, () => new Rectangle(0, TabHeight, mainLayout.Width, mainLayout.Height - TabHeight));

            //mix with ReactiveX
            /*Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => this._children.CollectionChanged += h,
                    h => this._children.CollectionChanged -= h)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args =>
                {
                    _contentView.Children.Clear();
                    _buttonStack.Children.Clear();
                    _buttonStack.ColumnDefinitions.Clear();

                    for (var ind = 0; ind < Children.Count; ind++)
                    {
                        _buttonStack.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Star});
                        var tabChild = Children[ind];
                        var tabItemControl = new TabBarButton(tabChild.Title, tabChild.ImageUrl,
                            tabChild.ImageDeactiveUrl,
                            tabChild.AccentColor);
                        if (FontFamily != null)
                            tabItemControl.FontFamily = FontFamily;

                        tabItemControl.FontSize = FontSize;
                        tabItemControl.SelectedColor = TabIndicatorColor;
                        var ind1 = ind;
                        tabItemControl.GestureRecognizers.Add(new TapGestureRecognizer
                        {
                            Command = new Command(() =>
                            {
                                Activate(Children[ind1], true);
                            })
                        });

                        Grid.SetColumn(tabItemControl, ind1);
                        _buttonStack.Children.Add(tabItemControl);
                    }

                    if (Children.Any())
                        Activate(Children.First(), true);
                });*/

            this._children.CollectionChanged += (s, e) =>
            {
                _contentView.Children.Clear();
                _buttonStack.Children.Clear();
                _buttonStack.ColumnDefinitions.Clear();

                for (var ind = 0; ind < Children.Count; ind++)
                {
                    _buttonStack.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Star});
                    var tabChild = Children[ind];
                    var tabItemControl = new TabBarButton(tabChild.Title, tabChild.ImageUrl,
                        tabChild.ImageDeactiveUrl,
                        tabChild.AccentColor);
                    if (FontFamily != null)
                        tabItemControl.FontFamily = FontFamily;

                    tabItemControl.FontSize = FontSize;
                    tabItemControl.SelectedColor = TabIndicatorColor;
                    var ind1 = ind;
                    tabItemControl.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(() => { Activate(Children[ind1], true); })
                    });

                    Grid.SetColumn(tabItemControl, ind1);
                    _buttonStack.Children.Add(tabItemControl);
                }

                if (Children.Any())
                    Activate(Children.First(), true);
            };

            //border
            var border = new BoxView
            {
                HeightRequest = 0.5,
                WidthRequest = mainLayout.Width,
                Color = Color.FromHex("#D0D2D2")
            };

            mainLayout.Children.Add(border, () => new Rectangle(
                0, TabHeight, mainLayout.Width, 1));

            //set content view
            Content = mainLayout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossTabStripControl"/> class.
        /// </summary>
        /// <param name="tabChild"></param>
        /// <param name="animate"></param>
        public void Activate(TabItem tabChild, bool animate)
        {
            var existingChild = Children.FirstOrDefault(t => t.View ==
                                                             _contentView.Children.FirstOrDefault(v => v.IsVisible));

            if (existingChild == tabChild)
                return;

            var idxOfExisting = existingChild != null ? Children.IndexOf(existingChild) : -1;
            var idxOfNew = Children.IndexOf(tabChild);

            if (idxOfExisting > -1 && animate)
            {
                _inTransition = true;

                // Animate
                var translation = idxOfExisting < idxOfNew
                    ? _contentView.Width
                    : -_contentView.Width;

                tabChild.View.TranslationX = translation;
                if (tabChild.View.Parent != _contentView)
                    _contentView.Children.Add(tabChild.View);
                else
                    tabChild.View.IsVisible = true;

                var newElementWidth = _buttonStack.Children.ElementAt(idxOfNew).Width;
                var newElementLeft = _buttonStack.Children.ElementAt(idxOfNew).X;

                var animation = new Animation();
                var existingViewOutAnimation = new Animation(d =>
                    {
                        if (existingChild != null)
                            existingChild.View.TranslationX = d;
                    },
                    0, -translation, Easing.CubicInOut, () =>
                    {
                        if (existingChild != null) existingChild.View.IsVisible = false;
                        _inTransition = false;
                    });

                var newViewInAnimation = new Animation(d => tabChild.View.TranslationX = d,
                    translation, 0, Easing.CubicInOut);

                var existingTranslation = _indicator.TranslationX;

                var indicatorTranslation = newElementLeft;
                var indicatorViewAnimation = new Animation(d => _indicator.TranslationX = d,
                    existingTranslation, indicatorTranslation, Easing.CubicInOut);

                var startWidth = _indicator.Width;
                var indicatorSizeAnimation = new Animation(d => _indicator.WidthRequest = d,
                    startWidth, newElementWidth, Easing.CubicInOut);

                animation.Add(0.0, 1.0, existingViewOutAnimation);
                animation.Add(0.0, 1.0, newViewInAnimation);
                animation.Add(0.0, 1.0, indicatorViewAnimation);
                animation.Add(0.0, 1.0, indicatorSizeAnimation);
                animation.Commit(this, "TabAnimation");
            }
            else
            {
                // Just set first view
                _contentView.Children.Clear();
                _contentView.Children.Add(tabChild.View);
            }

            foreach (var tabBtn in _buttonStack.Children)
                ((TabBarButton) tabBtn).IsSelected = _buttonStack.Children.IndexOf(tabBtn) == idxOfNew;

            if (idxOfNew != idxOfExisting && animate)
                RaiseTabActivated(idxOfNew);
        }

        /// <inheritdoc />
        /// <summary>
        /// Positions and sizes the children of a Layout.
        /// </summary>
        /// <remarks>Implementors wishing to change the default behavior of a Layout should override this method. It is suggested to
        /// still call the base method and modify its calculated results.</remarks>
        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            base.LayoutChildren(x, y, width, height);

            if (width > 0 && !_inTransition)
            {
                var existingChild = Children.FirstOrDefault(t =>
                    t.View == _contentView.Children.FirstOrDefault(v => v.IsVisible));

                var idxOfExisting = existingChild != null ? Children.IndexOf(existingChild) : -1;              

                _indicator.WidthRequest = _buttonStack.Children.ElementAt(idxOfExisting).Width;
                _indicator.TranslationX = _buttonStack.Children.ElementAt(idxOfExisting).X;
            }
        }

        /// <summary>
        /// Gets the views.
        /// </summary>
        /// <value>The views.</value>
        public new IList<TabItem> Children => _children;

        /// <summary>
        /// The FontSize property.
        /// </summary>
        public static BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(CrossTabStripControl), 14.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (CrossTabStripControl) bindable;
                    ctrl.FontSize = (double) newValue;
                });

        /// <summary>
        /// Gets or sets the FontSize of the TabBarButton instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public double FontSize
        {
            get => (double) GetValue(FontSizeProperty);
            set
            {
                SetValue(FontSizeProperty, value);
                foreach (var tabBtn in _buttonStack.Children)
                    ((TabBarButton) tabBtn).FontSize = value;
            }
        }

        /// <summary>
        /// The FontFamily property.
        /// </summary>
        public static BindableProperty FontFamilyProperty =
            BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(CrossTabStripControl), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (CrossTabStripControl) bindable;
                    ctrl.FontFamily = (string) newValue;
                });

        /// <summary>
        /// Gets or sets the FontFamily of the CrossTabStripControl instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public string FontFamily
        {
            get => (string) GetValue(FontFamilyProperty);
            set
            {
                SetValue(FontFamilyProperty, value);
                foreach (var tabBtn in _buttonStack.Children)
                    ((TabBarButton) tabBtn).FontFamily = value;
            }
        }

        /// <summary>
        /// The TabIndicatorColor property.
        /// </summary>
        public static BindableProperty TabIndicatorColorProperty =
            BindableProperty.Create(nameof(TabIndicatorColor), typeof(Color), typeof(CrossTabStripControl),
                Color.Accent,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (CrossTabStripControl) bindable;
                    ctrl.TabIndicatorColor = (Color) newValue;
                });

        /// <summary>
        /// Gets or sets the TabIndicatorColor of the CrossTabStripControl instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public Color TabIndicatorColor
        {
            get => (Color) GetValue(TabIndicatorColorProperty);
            set
            {
                SetValue(TabIndicatorColorProperty, value);
                _indicator.BackgroundColor = value;
            }
        }

        /// <summary>
        /// The TabHeight property.
        /// </summary>
        public static BindableProperty TabHeightProperty =
            BindableProperty.Create(nameof(TabHeight), typeof(double), typeof(CrossTabStripControl), 35.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (CrossTabStripControl) bindable;
                    ctrl.TabHeight = (double) newValue;
                });

        /// <summary>
        /// Gets or sets the TabHeight of the CrossTabStripControl instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public double TabHeight
        {
            get => (double) GetValue(TabHeightProperty);
            set => SetValue(TabHeightProperty, value);
        }

        /// <summary>
        /// The TabBackColor property.
        /// </summary>
        public static BindableProperty TabBackColorProperty =
            BindableProperty.Create(nameof(TabBackColor), typeof(Color), typeof(CrossTabStripControl),
                Color.FromHex("#f3f3f3"),
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (CrossTabStripControl) bindable;
                    ctrl.TabBackColor = (Color) newValue;
                });

        /// <summary>
        /// Gets or sets the TabBackColor of the CrossTabStripControl instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public Color TabBackColor
        {
            get => (Color) GetValue(TabBackColorProperty);
            set
            {
                SetValue(TabBackColorProperty, value);
                _tabControl.BackgroundColor = value;
            }
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
        }
    }

    /// <summary>
    /// Tab child.
    /// </summary>
    public class TabItem
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Title { get; set; }

        /// <summary>
        /// ImageUrl - url of image
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// ImageUrl - url of image
        /// </summary>
        public string ImageDeactiveUrl { get; set; }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>The view.</value>
        public View View { get; set; }

        /// <summary>
        /// get set AccentColor
        /// </summary>
        public Color AccentColor { get; set; } = Color.Accent;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabItem"/> class.
        /// </summary>
        public TabItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabItem"/> class.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="view">View.</param>
        public TabItem(string title, View view)
        {
            Title = title;
            View = view;
        }

        /// <summary>
        /// TabItem constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="view"></param>
        /// <param name="imageUrl"></param>
        public TabItem(string title, View view, string imageUrl)
        {
            Title = title;
            View = view;
            ImageUrl = imageUrl;
        }

        /// <summary>
        /// TabItem constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="view"></param>
        /// <param name="imageUrl"></param>
        /// <param name="imageDeactiveUrl"></param>
        public TabItem(string title, View view, string imageUrl, string imageDeactiveUrl)
        {
            Title = title;
            View = view;
            ImageUrl = imageUrl;
            ImageDeactiveUrl = imageDeactiveUrl;
        }

        /// <summary>
        /// TabItem constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="view"></param>
        /// <param name="imageUrl"></param>
        /// <param name="imageDeactiveUrl"></param>
        /// <param name="acentColor"></param>
        public TabItem(string title, View view, string imageUrl, string imageDeactiveUrl, Color acentColor)
        {
            Title = title;
            View = view;
            ImageUrl = imageUrl;
            ImageDeactiveUrl = imageDeactiveUrl;
            AccentColor = acentColor;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Tab bar indicator.
    /// </summary>
    public class TabBarIndicator : View
    {

    }

    /// <inheritdoc />
    /// <summary>
    /// Tab bar button.
    /// </summary>
    public class TabBarButton : ContentView
    {
        private readonly Label _label;
        public readonly CrossCachedImage CachedImage;
        public Color DarkTextColor = Color.Gray;
        public Color AccentColor = Color.Accent;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabBarButton"/> class.
        /// </summary>
        public TabBarButton()
        {
            _label = new Label
            {
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                FontSize = 14,
                LineBreakMode = LineBreakMode.TailTruncation,
                TextColor = DarkTextColor
            };

            CachedImage = new CrossCachedImage
            {
                WidthRequest = 25,
                HeightRequest = 25,
                HorizontalOptions = LayoutOptions.Center
            };

            var stackContent = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Padding = 5,
                Spacing = 5
            };

            stackContent.Children.Add(CachedImage);
            stackContent.Children.Add(_label);

            GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    _label.TextColor = AccentColor;
                })
            });

            //set content view
            Content = stackContent;
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:CrossTapStripControl.TabBarButton" /> class.
        /// </summary>
        public TabBarButton(string buttonText) : this()
        {
            _label.Text = buttonText;
        }

        public TabBarButton(string buttonText, string imageUrl) : this()
        {
            _label.Text = buttonText;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                CachedImage.ImageUrl = imageUrl;
                CachedImage.IsVisible = true;
            }
            else CachedImage.IsVisible = false;
        }

        public TabBarButton(string buttonText, string imageUrl, string imageDeactiveUrl) : this()
        {
            _label.Text = buttonText;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                CachedImage.ImageUrl = imageUrl;
                CachedImage.IsVisible = true;
            }
            else CachedImage.IsVisible = false;

            ImageUrl = imageUrl;
            ImageDeactiveUrl = imageDeactiveUrl;
        }

        public TabBarButton(string buttonText, string imageUrl, string imageDeactiveUrl, Color acentColor) : this()
        {
            _label.Text = buttonText;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                CachedImage.ImageUrl = imageUrl;
                CachedImage.IsVisible = true;
            }
            else CachedImage.IsVisible = false;

            ImageUrl = imageUrl;
            ImageDeactiveUrl = imageDeactiveUrl;

            AccentColor = acentColor;
        }

        /// <summary>
        /// The ButtonText property.
        /// </summary>
        public static BindableProperty ButtonTextProperty =
            BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(TabBarButton), null,
                BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (TabBarButton) bindable;
                    ctrl.ButtonText = (string) newValue;
                });

        /// <summary>
        /// Gets or sets the ButtonText of the TabBarButton instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public string ButtonText
        {
            get => (string) GetValue(ButtonTextProperty);
            set
            {
                SetValue(ButtonTextProperty, value);
                _label.Text = value;
            }
        }

        /// <summary>
        /// The SelectedColor property.
        /// </summary>
        public static BindableProperty SelectedColorProperty =
            BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(TabBarButton), Color.Accent,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (TabBarButton) bindable;
                    ctrl.SelectedColor = (Color) newValue;
                });

        /// <summary>
        /// Gets or sets the SelectedColor of the TabBarButton instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public Color SelectedColor
        {
            get => (Color) GetValue(SelectedColorProperty);
            set
            {
                SetValue(SelectedColorProperty, value);
                AccentColor = value;
            }
        }

        /// <summary>
        /// The FontSize property.
        /// </summary>
        public static BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TabBarButton), 14.0,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (TabBarButton) bindable;
                    ctrl.FontSize = (double) newValue;
                });

        /// <summary>
        /// Gets or sets the FontSize of the TabBarButton instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public double FontSize
        {
            get => (double) GetValue(FontSizeProperty);
            set
            {
                SetValue(FontSizeProperty, value);
                _label.FontSize = value;
            }
        }

        /// <summary>
        /// The FontFamily property.
        /// </summary>
        public static BindableProperty FontFamilyProperty =
            BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(TabBarButton), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var ctrl = (TabBarButton) bindable;
                    ctrl.FontFamily = (string) newValue;
                });

        /// <summary>
        /// Gets or sets the FontFamily of the TabBarButton instance.
        /// </summary>
        /// <value>The color of the buton.</value>
        public string FontFamily
        {
            get => (string) GetValue(FontFamilyProperty);
            set
            {
                SetValue(FontFamilyProperty, value);
                _label.FontFamily = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
        public bool IsSelected
        {
            get => _label.TextColor == AccentColor;
            set
            {
                _label.TextColor = value ? AccentColor : DarkTextColor;
                _label.FontAttributes = value ? FontAttributes.Bold : FontAttributes.None;

                if (!string.IsNullOrEmpty(ImageUrl) && !string.IsNullOrEmpty(ImageDeactiveUrl))
                {
                    //co mot buoc check neu set value la deactive va svg dang la url deactive thi khong can set lai nua
                    if (!value && CachedImage.ImageUrl.Equals(ImageDeactiveUrl)) return;
                    CachedImage.ImageUrl = value ? ImageUrl : ImageDeactiveUrl;
                }
            }
        }

        /// <summary>
        /// ImageUrl - url of image when tab active
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// ImageUrl - url of image when tab deactive
        /// </summary>
        public string ImageDeactiveUrl { get; set; }
    }

    public class CrossCachedImage : CachedImage
    {
        public CrossCachedImage()
        {
            CacheDuration = TimeSpan.FromDays(30);
            RetryCount = 3;
            RetryDelay = 250;
            DownsampleToViewSize = true;
            FixedOnMeasureBehavior = true;
            GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = ReactiveCommand.CreateFromTask(async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                    this.Command?.Execute(this.CommandParameter);
                })
            });
        }

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CrossCachedImage), null,
                BindingMode.TwoWay);

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(CrossCachedImage), null,
                BindingMode.TwoWay);

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(
            nameof(ImageUrl), typeof(string), typeof(CrossCachedImage), null, BindingMode.OneWay, null, OnImageUrlChanged);

        private static void OnImageUrlChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var url = (string)newValue;
            if (string.IsNullOrEmpty(url)) return;

            if(!(bindable is CrossCachedImage control)) return;
            control.Source = url;
        }

        public string ImageUrl
        {
            get => (string)GetValue(ImageUrlProperty);
            set => SetValue(ImageUrlProperty, value);
        }
    }
}