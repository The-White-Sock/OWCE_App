using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using OWCE.Views;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace OWCE.Pages
{
    [ContentProperty("BaseContent")]
    public class BaseContentPage : ContentPage
    {
        public static readonly BindableProperty BaseContentProperty = BindableProperty.Create(
            nameof(BaseContent),
            typeof(View),
            typeof(BaseContentPage));

        public View BaseContent
        {
            get { return (View)GetValue(BaseContentProperty); }
            set { SetValue(BaseContentProperty, value); }
        }



        public static readonly BindableProperty NavigationBarHeightProperty = BindableProperty.Create(
            nameof(NavigationBarHeight),
            typeof(int),
            typeof(BaseContentPage));

        public int NavigationBarHeight
        {
            get { return (int)GetValue(NavigationBarHeightProperty); }
            set { SetValue(NavigationBarHeightProperty, value); }
        }


        public static readonly BindableProperty SafeAreaInsetProperty = BindableProperty.Create(
            nameof(SafeAreaInsets),
            typeof(Thickness),
            typeof(BaseContentPage));

        public Thickness SafeAreaInsets
        {
            get { return (Thickness)GetValue(SafeAreaInsetProperty); }
            set { SetValue(SafeAreaInsetProperty, value); }
        }


        public static readonly BindableProperty CustomToolbarItemsProperty = BindableProperty.Create(
            nameof(CustomToolbarItems),
            typeof(ObservableCollection<CustomToolbarItem>),
            typeof(BaseContentPage));

        public ObservableCollection<CustomToolbarItem> CustomToolbarItems
        {
            get { return (ObservableCollection<CustomToolbarItem>)GetValue(CustomToolbarItemsProperty); }
            set { SetValue(CustomToolbarItemsProperty, value); }
        }


        readonly Grid _mainGrid;
        readonly BoxView _navBackgroundView;
        readonly Grid _navigationBarGrid;
        StackLayout _leftNavStackLayout;
        StackLayout _rightNavStackLayout;
        readonly Label _titleLabel;

        public BaseContentPage()
        {
            CustomToolbarItems = new ObservableCollection<CustomToolbarItem>();

            Xamarin.Forms.NavigationPage.SetHasNavigationBar(this, false);

            CustomToolbarItems.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    AddToolbarItems(e.NewItems);
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    RemoveToolbarItems(e.OldItems);
                }
                else
                {
                    RebuildToolbar();
                }
            };

            NavigationBarHeight = Device.RuntimePlatform switch
            {
                Device.iOS => 44,
                _ => 50,
            };

            _mainGrid = new Grid()
            {
                RowDefinitions = new RowDefinitionCollection()
                {
                    new RowDefinition() { Height = new GridLength(SafeAreaInsets.Top, GridUnitType.Absolute) },
                    new RowDefinition() { Height = new GridLength(NavigationBarHeight, GridUnitType.Absolute) },
                    new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) },
                },
                RowSpacing = 0,
                ColumnSpacing = 0,
            };

            _navBackgroundView = new BoxView();
            Grid.SetRowSpan(_navBackgroundView, 2);
            Grid.SetRow(_navBackgroundView, 0);
            _mainGrid.Children.Add(_navBackgroundView);

            _navigationBarGrid = new Grid();
            Grid.SetRow(_navigationBarGrid, 1);
            _mainGrid.Children.Add(_navigationBarGrid);

            _titleLabel = new Label()
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
            };

            Grid.SetRow(_titleLabel, 1);
            _mainGrid.Children.Add(_titleLabel);

            UpdateThemedColors();

            Content = _mainGrid;
        }

        // These are built in C# rather than XAML, so they don't get the automatic
        // live theme-reactivity {AppThemeBinding ...} gives everything else (#35) -
        // refreshed explicitly on OnAppearing/OnDisappearing's subscription below,
        // since this same page instance can otherwise sit visible for a long time
        // (BoardListPage is the app's root page) while the rider changes the theme
        // from Settings in a separate modal.
        //
        // _mainGrid's own background matters here too, not just belt-and-suspenders:
        // App.xaml's implicit Style TargetType="ContentPage" never actually applies to
        // this class, since Xamarin.Forms implicit styles only match a resource's exact
        // TargetType, not subclasses, without ApplyToDerivedTypes="True". Pages whose
        // own content already paints a full-bleed background (BoardPage/BoardListPage's
        // gradient Grid) hide that gap by accident; pages that don't (AppSettingsPage,
        // SubmitRidePage, PastRidesPage, AboutPage) were left showing the platform's
        // true default (light) page background under theme-aware dark-mode text.
        void UpdateThemedColors()
        {
            Color darkBackground = (App.Current.Resources["darkBackgroundColor"] as Color?) ?? Color.Black;

            _mainGrid.BackgroundColor = ThemeHelper.Pick(Color.White, darkBackground);

            _navBackgroundView.BackgroundColor = ThemeHelper.Pick(
                (App.Current.Resources["BackgroundGradientStart"] as Color?) ?? Color.White,
                darkBackground);

            _titleLabel.TextColor = ThemeHelper.Pick(Color.Black, Color.White);
        }

        void OnRequestedThemeChanged(object sender, AppThemeChangedEventArgs e)
        {
            UpdateThemedColors();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // A page sitting behind a pushed modal (eg BoardPage behind AppSettingsPage)
            // gets OnDisappearing'd - and unsubscribed - while the modal is up. If the
            // theme changes while it's hidden, it misses RequestedThemeChanged entirely;
            // re-subscribing on return only catches *future* changes, not the one that
            // already happened. Refresh unconditionally so it can't come back stale.
            UpdateThemedColors();
            Xamarin.Forms.Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Xamarin.Forms.Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }


        protected override void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanging(propertyName);

            if (BaseContentProperty.PropertyName.Equals(propertyName, StringComparison.Ordinal))
            {
                if (BaseContent != null)
                {
                    _mainGrid.Children.Remove(BaseContent);
                }
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (Xamarin.Forms.PlatformConfiguration.iOSSpecific.Page.SafeAreaInsetsProperty.PropertyName.Equals(propertyName, StringComparison.Ordinal))
            {
                SafeAreaInsets = On<Xamarin.Forms.PlatformConfiguration.iOS>().SafeAreaInsets();
                _mainGrid.RowDefinitions[0].Height = new GridLength(SafeAreaInsets.Top, GridUnitType.Absolute);
            }
            else if (BaseContentProperty.PropertyName.Equals(propertyName, StringComparison.Ordinal))
            {
                Grid.SetRow(BaseContent, 2);
                _mainGrid.Children.Add(BaseContent);
            }
            else if (TitleProperty.PropertyName.Equals(propertyName, StringComparison.Ordinal))
            {
                _titleLabel.Text = Title;
            }
        }

        void AddToolbarItems(IList newItems)
        {
            foreach (CustomToolbarItem customToolbarItem in newItems)
            {
                AddToolbarItem(customToolbarItem);
            }
        }

        void AddToolbarItem(CustomToolbarItem newItem)
        {
            if (newItem.Parent != null)
            {
                return;
            }

            if (newItem.Position == CustomToolbarItemPosition.Left)
            {
                SetupLeftNavStackLayout();

                if (_leftNavStackLayout.Children.Count == 0)
                {
                    _leftNavStackLayout.Children.Add(newItem);
                }
                else
                {
                    for (int i = 0; i < _leftNavStackLayout.Children.Count; ++i)
                    {
                        if (_leftNavStackLayout.Children[i] is CustomToolbarItem customToolbarItem)
                        {
                            if (newItem.Priority > customToolbarItem.Priority)
                            {
                                _leftNavStackLayout.Children.Insert(i, newItem);
                                break;
                            }
                        }
                    }

                    // newItem.Parent is set as soon as it's added to a Layout's Children.
                    // If the loop above already Insert()'d it, Parent is non-null here and
                    // adding it again would double-add (Xamarin.Forms rejects re-parenting
                    // an element that already has a parent). If the loop never found a
                    // lower-priority item to insert before, Parent is still null - that's
                    // when it needs to be appended at the end. This condition was
                    // previously inverted, which double-added or silently dropped items.
                    if (newItem.Parent == null)
                    {
                        _leftNavStackLayout.Children.Add(newItem);
                    }
                }
            }
            else if (newItem.Position == CustomToolbarItemPosition.Right)
            {
                SetupRightNavStackLayout();

                if (_rightNavStackLayout.Children.Count == 0)
                {
                    _rightNavStackLayout.Children.Add(newItem);
                }
                else
                {
                    for (int i = _rightNavStackLayout.Children.Count - 1; i >= 0; --i)
                    {
                        if (_rightNavStackLayout.Children[i] is CustomToolbarItem customToolbarItem)
                        {
                            if (newItem.Priority > customToolbarItem.Priority)
                            {
                                _rightNavStackLayout.Children.Insert(i + 1, newItem);
                                break;
                            }
                        }
                    }

                    // Same fix as the left-side branch above - see its comment.
                    if (newItem.Parent == null)
                    {
                        _rightNavStackLayout.Children.Add(newItem);
                    }
                }
            }
        }

        static void RemoveToolbarItems(IList oldItems)
        {
            foreach (CustomToolbarItem customToolbarItem in oldItems)
            {
                RemoveToolbarItem(customToolbarItem);
            }
        }

        static void RemoveToolbarItem(CustomToolbarItem oldItem)
        {
            if (oldItem.Parent is StackLayout stackLayout)
            {
                stackLayout.Children.Remove(oldItem);
            }
        }

        void RebuildToolbar()
        {
            _leftNavStackLayout?.Children.Clear();
            _rightNavStackLayout?.Children.Clear();

            AddToolbarItems(CustomToolbarItems);
        }

        private void SetupLeftNavStackLayout()
        {
            if (_leftNavStackLayout != null)
            {
                return;
            }

            _leftNavStackLayout = new StackLayout()
            {
                HorizontalOptions = LayoutOptions.Start,
                Spacing = 8,
                Margin = new Thickness(16, 0, 0, 0),
            };

            _navigationBarGrid.Children.Insert(0, _leftNavStackLayout);
        }

        private void SetupRightNavStackLayout()
        {
            if (_rightNavStackLayout != null)
            {
                return;
            }

            _rightNavStackLayout = new StackLayout()
            {
                HorizontalOptions = LayoutOptions.End,
                Spacing = 8,
                Margin = new Thickness(0, 0, 16, 0),
            };

            _navigationBarGrid.Children.Add(_rightNavStackLayout);
        }

        protected Label GetTitleLabel()
        {
            return _titleLabel;
        }

    }
}
