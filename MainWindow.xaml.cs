﻿using SaberColorfulStartmenu.Core;
using SaberColorfulStartmenu.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Animation;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using __WinForm = System.Windows.Forms;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using File = System.IO.File;
using Image = System.Windows.Controls.Image;

namespace SaberColorfulStartmenu
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        #region Fields

        private List<string> _fileList = new List<string>();
        private List<BitmapSource> _logoList = new List<BitmapSource>();
        private bool _saveFlag;
        private bool _loaded;
        private bool _sysChangeing;
        private Color _currentColor = Colors.Black;
        private string _currentColorString;
        private int _currentId = -1;
        private __WinForm.ColorDialog _colorDialog;
        private OpenFileDialog _openFile;
        private BitmapSource _logo;
        private StartmenuShortcutInfo _currentInfo;
        private string _newLogoLoc;

        #endregion

        public MainWindow()
        {
            _colorDialog = new __WinForm.ColorDialog();
            _openFile = new OpenFileDialog();

            InitializeComponent();
            _colorDialog.AllowFullOpen = true;
            _colorDialog.AnyColor = true;
            _colorDialog.FullOpen = true;
            _openFile.AddExtension = true;
            _openFile.Filter = "图像文件|*.png;*.jpg;*.jpeg;*.gif";
            RefreshList();
        }

        #region Events

        private void CloseBtn_OnClick(object sender, RoutedEventArgs e) => Close();

        private void MinBtn_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Main_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            SaveCheck();
            appList.Items.Clear();
            RefreshList();
        }

        private void About_OnClick(object sender, RoutedEventArgs e) => new AboutWindow().Show();

        private void ButtonBase_OnClick_4(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", App.CommonStartMenu);
            Process.Start("explorer", App.StartMenu);
        }

        private void AppList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (appList.SelectedIndex == _currentId) return;
            if (appList.SelectedIndex == -1) {
                gridSetMain.Visibility = Visibility.Collapsed;
                return;
            }

            gridSetMain.Visibility = Visibility.Visible;
            //保存。。
            if (!SaveCheck()) {
                appList.SelectedIndex = _currentId;
                return;
            }

            var first = _currentId == -1;
            _currentId = appList.SelectedIndex;
            if (first) {
                ChangeStory_OnCompleted(null, null);
            }
            else {
                var csb = (Storyboard)Resources["ChangeStory_1"];
                csb.Begin();
            }

            //Load();
        }
        //
        //        private void Selector_OnSelectionChanged_1(object sender, SelectionChangedEventArgs e)
        //        {
        //            if (!_loaded) return;
        //            if (!_sysChangeing) _saveFlag = true;
        //            if (!(modeCheck.IsChecked ?? false)) {
        //                colorSelector.Visibility = Visibility.Collapsed;
        //                group_Color.Visibility = Visibility.Collapsed;
        //            }
        //            else {
        //                colorSelector.Visibility = Visibility.Visible;
        //                // ReSharper disable once PossibleUnintendedReferenceComparison
        //                //                if (colorSelector.SelectedItem == defineColorItem)
        //                //                {
        //                //                    group_Color.Visibility = Visibility.Visible;
        //                //                }
        //                group_Color.Visibility =
        //                    colorSelector_17.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;
        //            }
        //        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;
            gridSetMain.Visibility = Visibility.Hidden;
            _sysChangeing = true;
            //Selector_OnSelectionChanged_1(sender, null);
            ToggleButton_OnChecked(this, null);
            _sysChangeing = false;
        }

        private void ButtonBase_OnClick_5(object sender, RoutedEventArgs e)
        {
            if (_colorDialog.ShowDialog() != __WinForm.DialogResult.OK) return;
            _saveFlag = true;
            _currentColor = _colorDialog.Color.ToMediaColor();
            _currentColorString = _currentColor.ToRgbString();
            defineColorText.Text = _currentColorString;
            UpdateRender();
        }

        private void DefineColorText_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_sysChangeing) return;
            if (defineColorText.Text.Length != 7 || !defineColorText.Text.StartsWith("#")) return;
            _saveFlag = true;
            try {
                // ReSharper disable once PossibleNullReferenceException
                _currentColor = Helper.GetColorFromRgbString(defineColorText.Text);
                _currentColorString = defineColorText.Text;
                defineColorText.Foreground = Brushes.Black;
                defineColorText.ToolTip = null;
                UpdateRender();
            }
            catch (FormatException) {
                defineColorText.Foreground = Brushes.DeepPink;
                defineColorText.ToolTip = "格式错误.";
                Debug.WriteLine("ChangeColor Canceled.");
            }
        }

        private void SaveAndUpdate_RoutedEvent(object sender, RoutedEventArgs e)
        {
            if (!_loaded || _sysChangeing) return;
            _saveFlag = true;
            UpdateRender();
        }

        private void ButtonBase_OnClick_7(object sender, RoutedEventArgs e)
        {
            _openFile.Title = "选择150x150大图标 建议比例：1：1";
            if (!(_openFile.ShowDialog() ?? false)) return;
            try {
                var fs = File.Open(_openFile.FileName, FileMode.Open, FileAccess.Read);
                _saveFlag = true;
                _newLogoLoc = _openFile.FileName;
                _logo = new Bitmap(fs).ToBitmapSource();
                UpdateRender();
                fs.Close();
            }
            catch (Exception ex) {
                MessageBox.Show($"图片载入失败\n未知错误，无法读取此文件\n详细信息：{ex.Message}", "错误", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ButtonBase_OnClick_6(object sender, RoutedEventArgs e)
        {
            if (Save())
                ((Storyboard)Resources["saveDoneStory"]).Begin();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e) =>
            e.Cancel = !SaveCheck();

        private void ChangeStory_OnCompleted(object sender, EventArgs e)
        {
            Load();
            var sb = (Storyboard)Resources["ChangeStory_2"];
            sb.Begin();
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
        }

        private void ColorSelector_OnChecked(object sender, RoutedEventArgs e)
        {
            // ReSharper disable PossibleUnintendedReferenceComparison
            if (!_loaded) return;
            group_Color.Visibility = colorSelector_17.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;
            if (!_sysChangeing) _saveFlag = true;
            if (sender == colorSelector_1) {
                _currentColorString = "black";
                _currentColor = Colors.Black;
            }
            else if (sender == colorSelector_2) {
                _currentColorString = "silver";
                _currentColor = Colors.Silver;
            }
            else if (sender == colorSelector_3) {
                _currentColorString = "gray";
                _currentColor = Colors.Gray;
            }
            else if (sender == colorSelector_4) {
                _currentColorString = "white";
                _currentColor = Colors.White;
            }
            else if (sender == colorSelector_5) {
                _currentColorString = "maroon";
                _currentColor = Colors.Maroon;
            }
            else if (sender == colorSelector_6) {
                _currentColorString = "red";
                _currentColor = Colors.Red;
            }
            else if (sender == colorSelector_7) {
                _currentColorString = "purple";
                _currentColor = Colors.Purple;
            }
            else if (sender == colorSelector_8) {
                _currentColorString = "fuchsia";
                _currentColor = Colors.Fuchsia;
            }
            else if (sender == colorSelector_9) {
                _currentColorString = "green";
                _currentColor = Colors.Green;
            }
            else if (sender == colorSelector_10) {
                _currentColorString = "lime";
                _currentColor = Colors.Lime;
            }
            else if (sender == colorSelector_11) {
                _currentColorString = "olive";
                _currentColor = Colors.Olive;
            }
            else if (sender == colorSelector_12) {
                _currentColorString = "yellow";
                _currentColor = Colors.Yellow;
            }
            else if (sender == colorSelector_13) {
                _currentColorString = "navy";
                _currentColor = Colors.Navy;
            }
            else if (sender == colorSelector_14) {
                _currentColorString = "blue";
                _currentColor = Colors.Blue;
            }
            else if (sender == colorSelector_15) {
                _currentColorString = "teal";
                _currentColor = Colors.Teal;
            }
            else if (sender == colorSelector_16) {
                _currentColorString = "aqua";
                _currentColor = Colors.Aqua;
            }
            else if (!_sysChangeing && sender == colorSelector_17) {
                //自定义
                try {
                    _currentColor = Helper.GetColorFromRgbString(defineColorText.Text);
                    _currentColorString = defineColorText.Text;
                }
                catch (FormatException) {
                    defineColorText.Text = "#000000";
                    _currentColorString = "black";
                    _currentColor = Colors.Black;
                }
            }

            if (!_sysChangeing) UpdateRender();
            // ReSharper restore PossibleUnintendedReferenceComparison
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (!_sysChangeing) _saveFlag = true;
            if (!(modeCheck.IsChecked ?? false)) {
                colorSelector.Visibility = Visibility.Collapsed;
                group_Color.Visibility = Visibility.Collapsed;
            }
            else {
                colorSelector.Visibility = Visibility.Visible;
                group_Color.Visibility =
                    colorSelector_17.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Functions

        private void RefreshList()
        {
            var unknown = Properties.Resources.unknown.ToBitmapSource();
            _fileList.Clear();
            appList.Items.Clear();
            _logoList.Clear();
            GC.Collect();
            _currentId = -1;
            _fileList.AddRange(Helper.GetAllFilesByDir(App.StartMenu));
            _fileList.AddRange(Helper.GetAllFilesByDir(App.CommonStartMenu));
            _fileList.RemoveAll(str => !str.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase));
            for (var i = 0; i < _fileList.Count; i++) {
                WshShortcut shortcut = Helper.MainShell.CreateShortcut(_fileList[i]);
                var target = Helper.ConvertEnviromentArgsInPath(shortcut.TargetPath);
                __tf:
                Debug.WriteLine(target);
                if ((!target.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) || !File.Exists(target)) {
                    if (target.ToLower().Contains("program files (x86)")) {
                        //Reason
                        //实测有部分应用（这包括Microsoft Office） 的快捷方式在使用任何一种Wshshell（这包括C# 的WshShortcut和C++的shlobj.h）获取TargetPath时
                        //Program Files 都有几率变为 Program Files (x86) 暂时不了解原因，网上也没有相关的错误报告
                        //msdn居然对IWshShell一个文档都没有= = 
                        //这种临时的解决方式，只能算是一种下下策了吧 =。=
                        //如果有知道解决方案的可以当issue汇报
                        //阿里嘎多

                        target = target.ToLower().Replace("program files (x86)", "program files");
                        goto __tf;
                    }

                    Debug.WriteLine("Torow!!!");
                    _fileList.RemoveAt(i);
                    i--;
                    continue;
                }

                string iconPath;
                int iconId;
                if (shortcut.IconLocation.Trim().StartsWith(",")) {
                    //targetpath对应icon
                    iconId = int.Parse(shortcut.IconLocation.Substring(1));
                    iconPath = target;
                }
                else {
                    var tmp = shortcut.IconLocation.Split(',');

                    iconId = int.Parse(tmp[tmp.Length - 1]);
                    iconPath = Helper.ConvertEnviromentArgsInPath(
                        shortcut.IconLocation.Replace($",{iconId}", string.Empty));
                }

                if (iconPath.EndsWith(".exe") || iconPath.EndsWith(".dll")) {
                    try {
                        var icons = Helper.GetLargeIconsFromExeFile(iconPath);
                        _logoList.Add(icons[iconId].ToBitmap().ToBitmapSource());

                        foreach (var item in icons) {
                            Helper.DestroyIcon(item.Handle);
                            item.Dispose();
                        }
                    }
                    catch {
                        _logoList.Add(unknown);
                    }
                }
                else {
                    //ico
                    try {
                        var ico = new Icon(iconPath);
                        _logoList.Add(ico.ToBitmap().ToBitmapSource());
                        ico.Dispose();
                    }
                    catch {
                        _logoList.Add(unknown);
                    }
                }

                Debug.WriteLine($"icon id:{iconId};icon path:{iconPath}");
                var itemName = Path.GetFileNameWithoutExtension(_fileList[i]);
                // ReSharper disable once AssignNullToNotNullAttribute
                if (App.charMap_Cn.ContainsKey(itemName))
                    itemName = App.charMap_Cn[itemName];
                var lvi = new ListViewItem();
                var sp = new StackPanel() {
                    Orientation = Orientation.Horizontal,
                    Height = 25
                };
                sp.Children.Add(new Image() {
                    Source = _logoList[i]
                });
                sp.Children.Add(new TextBlock() {Text = itemName, FontSize = 14});
                lvi.Content = sp;
                appList.Items.Add(lvi);
            }

            //获取所有子目录内容
            //只监视.lnk文件
        }

        private void Load()
        {
            _sysChangeing = true;

            _currentInfo = new StartmenuShortcutInfo(_fileList[_currentId]);
            if (_currentInfo.XmlFile == null) {
                modeCheck.IsChecked = false;
                //set everything to empty
                _currentColor = Colors.Black;
                _currentColorString = "black";
                _logo = null;
                defineIconCheck.IsChecked = false;
                //txtColorSelector.SelectedIndex = 0;
                txtWhiteColor.IsChecked = true;
                //colorSelector.SelectedIndex = 0;
                colorSelector_1.IsChecked = true;
            }
            else {
                //modeSelctor.SelectedIndex = 1;
                modeCheck.IsChecked = true;
                try {
                    switch (_currentInfo.XmlFile.ColorStr) {
                        case "black":
                            //                            _currentColor = Colors.Black;
                            colorSelector_1.IsChecked = true;
                            break;
                        case "silver":
                            //                            _currentColor = Colors.Silver;
                            colorSelector_2.IsChecked = true;
                            break;
                        case "gray":
                            //                            _currentColor = Colors.Gray;
                            colorSelector_3.IsChecked = true;
                            break;
                        case "white":
                            //                            _currentColor = Colors.White;
                            colorSelector_4.IsChecked = true;
                            break;
                        case "maroon":
                            //                            _currentColor = Colors.Maroon;
                            colorSelector_5.IsChecked = true;
                            break;
                        case "red":
                            //                            _currentColor = Colors.Red;
                            colorSelector_6.IsChecked = true;
                            break;
                        case "purple":
                            //                            _currentColor = Colors.Purple;
                            colorSelector_7.IsChecked = true;
                            break;
                        case "fuchsia":
                            //                            _currentColor = Colors.Fuchsia;
                            colorSelector_8.IsChecked = true;
                            break;
                        case "green":
                            //                            _currentColor = Colors.Green;
                            colorSelector_9.IsChecked = true;
                            break;
                        case "lime":
                            //                            _currentColor = Colors.Lime;
                            colorSelector_10.IsChecked = true;
                            break;
                        case "olive":
                            //                            _currentColor = Colors.Olive;
                            colorSelector_11.IsChecked = true;
                            break;
                        case "yellow":
                            //                            _currentColor = Colors.Yellow;
                            colorSelector_12.IsChecked = true;
                            break;
                        case "navy":
                            //                            _currentColor = Colors.Navy;
                            colorSelector_13.IsChecked = true;
                            break;
                        case "blue":
                            //                            _currentColor = Colors.Blue;
                            colorSelector_14.IsChecked = true;
                            break;
                        case "teal":
                            //                            _currentColor = Colors.Teal;
                            colorSelector_15.IsChecked = true;
                            break;
                        case "aqua":
                            //                            _currentColor = Colors.Aqua;
                            colorSelector_16.IsChecked = true;
                            break;
                        default:
                            try {
                                _currentColor = Helper.GetColorFromRgbString(_currentInfo.XmlFile.ColorStr);
                                _currentColorString = _currentInfo.XmlFile.ColorStr;
                                colorSelector_17.IsChecked = true;
                                defineColorText.Text = _currentInfo.XmlFile.ColorStr;
                            }
                            catch (FormatException) {
                                _currentInfo.XmlFile.ColorStr = "black";
                                _currentColor = Colors.Black;
                                colorSelector_1.IsChecked = true;
                            }

                            break;
                    }

                    //                    if (!string.IsNullOrEmpty(_nowInfo.XmlFile.LargeLogoLoc))
                    //                    {
                    //                        if (File.Exists(_nowInfo.XmlFile.LargeLogoLoc))
                    //                        {
                    //                            Debug.WriteLine(
                    //                                $"Load large icon successfully with file location{_nowInfo.XmlFile.LargeLogoLoc}");
                    //                            _logo = new Bitmap(_nowInfo.XmlFile.LargeLogoLoc);
                    //                        }
                    //                        else if (Directory.Exists(Path.GetDirectoryName(_nowInfo.XmlFile.LargeLogoLoc)))
                    //                        {
                    //                            var files = Directory.GetFiles(Path.GetDirectoryName(_nowInfo.XmlFile.LargeLogoLoc));
                    //                            // ReSharper disable once AssignNullToNotNullAttribute
                    //                            var regex = new Regex(Path.Combine(Path.GetDirectoryName(_nowInfo.XmlFile.LargeLogoLoc),
                    //                                                      Path.GetFileNameWithoutExtension(_nowInfo.XmlFile
                    //                                                          .LargeLogoLoc)).RegexFree() + "\\.scale-\\d+\\" +
                    //                                                  Path.GetExtension(_nowInfo.XmlFile.LargeLogoLoc));
                    //                            var lastFileName = files.LastOrDefault(a => regex.IsMatch(a));
                    //                            if (!string.IsNullOrEmpty(lastFileName))
                    //                            {
                    //                                Debug.WriteLine($"Load large icon successfully with file location{lastFileName}");
                    //                                _largeIcon = new Bitmap(lastFileName);
                    //                            }
                    //                        }
                    //                    }
                    //                    var tryLarge = false;
                    //                    var tryDir = true;

                    if (string.IsNullOrEmpty(_currentInfo.XmlFile.SmallLogoLoc) &&
                        !string.IsNullOrEmpty(_currentInfo.XmlFile.LargeLogoLoc)) {
                        //replace smallLogo with largeLogo
                        _currentInfo.XmlFile.SmallLogoLoc = _currentInfo.XmlFile.LargeLogoLoc;
                    }

                    if (!string.IsNullOrEmpty(_currentInfo.XmlFile.SmallLogoLoc)) {
                        if (File.Exists(_currentInfo.XmlFile.GetFullPath(_currentInfo.XmlFile.SmallLogoLoc))) {
                            //直接获取
                            Debug.WriteLine(
                                $"Load small icon successfully with file location{_currentInfo.XmlFile.SmallLogoLoc}");
                            _logo = new Bitmap(_currentInfo.XmlFile.GetFullPath(_currentInfo.XmlFile.SmallLogoLoc))
                                .ToBitmapSource();
                            //_scaleMode = false;
                            defineIconCheck.IsChecked = true;
                            btnChangeLogo.Visibility = Visibility.Visible;
                            txtDevDefIco.Visibility = Visibility.Collapsed;
                            goto __hasLogo;
                        }
                        else if (Directory.Exists(
                                     Path.GetDirectoryName(
                                         _currentInfo.XmlFile.GetFullPath(_currentInfo.XmlFile.SmallLogoLoc))) &&
                                 // ReSharper disable once AssignNullToNotNullAttribute
                                 File.Exists(Path.Combine(Path.GetDirectoryName(_currentInfo.Target),
                                     "Resources.pri"))) {
                            //scale模式
                            //_scaleMode = true;
                            _logo = null;
                            defineIconCheck.IsChecked = true;
                            btnChangeLogo.Visibility = Visibility.Collapsed;
                            txtDevDefIco.Visibility = Visibility.Visible;
                            goto __hasLogo;
                        }
                        else {
                            //异常，清除
                            _currentInfo.XmlFile.SmallLogoLoc = _currentInfo.XmlFile.LargeLogoLoc = null;
                        }
                    }

                    _logo = null;
                    //_scaleMode = false;
                    defineIconCheck.IsChecked = false;
                    btnChangeLogo.Visibility = Visibility.Visible;
                    txtDevDefIco.Visibility = Visibility.Collapsed;
                    __hasLogo:
                    //txtColorSelector.SelectedIndex = (int)_currentInfo.XmlFile.TxtForeground;
                    if (_currentInfo.XmlFile.TxtForeground == StartmenuXmlFile.TextCol.light)
                        txtWhiteColor.IsChecked = true;
                    else txtBlackColor.IsChecked = true;
                }
#if DEBUG
                catch (Exception e) {
                    Debug.WriteLine("-----EXCEPTION-----");
                    Debug.WriteLine(e);
                    Debug.WriteLine("--------END--------");
#else
                catch {
#endif
                    // ReSharper disable once HeuristicUnreachableCode
                    MessageBox.Show("读取配置文件时发生错误\n已重置到初始值", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    File.Delete(_currentInfo.XmlFileLocation);
                    Load();
                    return;
                }
            }

            UpdateRender();
            _sysChangeing = false;
        }

        private void UpdateRender()
        {
            Debug.WriteLine(DateTime.Now + " An Render update queue.");
            defineColorPreview.Fill = new SolidColorBrush(_currentColor);
            previewColor.Color = _currentColor;
            preview_LargeText.Foreground = (txtWhiteColor.IsChecked ?? false) ? Brushes.White : Brushes.Black;
            preview_LargeText.Visibility =
                largeAppNameCheck.IsChecked ?? false ? Visibility.Visible : Visibility.Hidden;
            if (_logo == null || !(defineIconCheck.IsChecked ?? false)) {
                preview_SmallImg.Source = preview_LargeImg.Source = _logoList[_currentId];
                preview_SmallImg.Stretch = preview_LargeImg.Stretch = Stretch.None;
            }
            else {
                preview_SmallImg.Source = preview_LargeImg.Source = _logo;
                preview_SmallImg.Stretch =
                    (_logo.PixelWidth > 70 || _logo.PixelHeight > 70) ? Stretch.Uniform : Stretch.None;
                preview_LargeImg.Stretch =
                    (_logo.PixelWidth > 150 || _logo.PixelHeight > 150) ? Stretch.Uniform : Stretch.None;
            }

            preview_LargeText.Text = Path.GetFileNameWithoutExtension(_fileList[_currentId]);
        }

        private bool SaveCheck()
        {
            if (!_saveFlag || _currentId == -1) return true;
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (MessageBox.Show("更改尚未保存，是否保存？", "提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Asterisk,
                MessageBoxResult.Cancel)) {
                case MessageBoxResult.Yes:
                    return Save();
                case MessageBoxResult.No:
                    _saveFlag = false;
                    return true;
                default:
                    return false;
            }
        }

        private bool Save()
        {
            if (_currentId == -1) return true;
            //todo save
            if (_saveFlag) {
                if (!(modeCheck.IsChecked ?? false)) {
                    if (_currentInfo.XmlFile != null) {
                        if (MessageBox.Show("将删除所有自定义效果文件\n继续?", "⚠警告", MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) != MessageBoxResult.Yes) return false;
                        _currentInfo.XmlFile = null;
                        File.Delete(_currentInfo.XmlFileLocation);
                    }
                }
                else {
                    if (_currentInfo.XmlFile == null) {
                        _currentInfo.XmlFile = new StartmenuXmlFile(_currentInfo.XmlFileLocation);
                    }

                    _currentInfo.XmlFile.ColorStr = _currentColorString;
                    _currentInfo.XmlFile.TxtForeground = (txtWhiteColor.IsChecked ?? false)
                        ? StartmenuXmlFile.TextCol.light
                        : StartmenuXmlFile.TextCol.dark;
                    _currentInfo.XmlFile.ShowTitleOnLargeIcon = largeAppNameCheck.IsChecked ?? false;
                    //保存图片
                    var sha1 = SHA1.Create();
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var pathName = Path.Combine(Path.GetDirectoryName(_currentInfo.XmlFileLocation),
                        "__StartmenuIcons__");
                    if (!Directory.Exists(pathName))
                        Directory.CreateDirectory(pathName);

                    //文件名：[DIR]\__StartmenuIcons__\[SHA1].png
                    if (defineIconCheck.IsChecked ?? false) {
                        if (!string.IsNullOrEmpty(_newLogoLoc)) {
                            //计算SHA1
                            var fs = File.Open(_newLogoLoc, FileMode.Open, FileAccess.Read);
                            var hash = BitConverter.ToString(sha1.ComputeHash(fs)).Replace("-", string.Empty).ToLower();
                            fs.Close();
                            var fileName = Path.Combine(pathName, hash) + Path.GetExtension(_newLogoLoc);
                            var fileNameWithoutDir = Path.Combine("__StartmenuIcons__", hash) +
                                                     Path.GetExtension(_newLogoLoc).ToLower();
                            if (!File.Exists(fileName)) //拷贝图像到目录
                                File.Copy(_newLogoLoc, fileName);
                            _currentInfo.XmlFile.SmallLogoLoc = _currentInfo.XmlFile.LargeLogoLoc = fileNameWithoutDir;
                        }
                        else if (string.IsNullOrEmpty(_currentInfo.XmlFile.SmallLogoLoc)) {
                            MessageBox.Show("需要指定作为图标的图片。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }
                    else {
                        _currentInfo.XmlFile.LargeLogoLoc = _currentInfo.XmlFile.SmallLogoLoc = string.Empty;
                    }

                    _currentInfo.XmlFile.Save();
                }

                _saveFlag = false;
            }

            //Update file and let the explorer reload the link
            Helper.UpdateFile(_currentInfo.Location);
            return true;
        }

        #endregion
    }
}
