using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Views.Dialogs;

/// <summary>
/// AI 助手独立窗口 - 支持会话历史、快捷操作等功能。
/// </summary>
public sealed partial class AIAssistantWindow : Window, INotifyPropertyChanged
{
    private readonly IAIAssistant _aiAssistant;
    private readonly AIAssistantContext _context;
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _cts;
    private AppWindow? _appWindow;

    // 会话历史
    public ObservableCollection<ChatSession> Sessions { get; } = new();
    private ChatSession? _currentSession;

    public ObservableCollection<AIMessageItem> Messages { get; } = new();

    private bool _isInputEnabled = true;
    public bool IsInputEnabled
    {
        get => _isInputEnabled;
        set { _isInputEnabled = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AIAssistantWindow(IAIAssistant aiAssistant, AIAssistantContext context)
    {
        _aiAssistant = aiAssistant;
        _context = context;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        this.InitializeComponent();
        
        SetupWindow();
        SetupModuleUI();
        SetupQuickActions();
        CreateNewSession();
    }

    private void SetupWindow()
    {
        // 获取 AppWindow 以自定义窗口
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            // 设置窗口大小
            _appWindow.Resize(new SizeInt32(1100, 750));
            
            // 设置标题栏
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = _appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            }

            // 设置窗口标题
            _appWindow.Title = GetWindowTitle();
        }

        // 设置拖拽区域
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
    }

    private string GetWindowTitle()
    {
        return _context.Module switch
        {
            AIModule.SoftwareList => "软件分析助手",
            AIModule.FileManager => "文件整理助手",
            AIModule.Cleanup => "清理助手",
            AIModule.Migration => "迁移助手",
            _ => "AI 助手"
        };
    }

    private void SetupModuleUI()
    {
        var (title, subtitle, icon) = _context.Module switch
        {
            AIModule.SoftwareList => ("软件分析助手", $"已加载 {_context.SelectedSoftware?.Count ?? 0} 个软件", "\uE74C"),
            AIModule.FileManager => ("文件整理助手", _context.CurrentPath ?? "未选择目录", "\uE8B7"),
            AIModule.Cleanup => ("清理助手", $"已扫描 {_context.CleanupItems?.Count ?? 0} 个项目", "\uE74D"),
            AIModule.Migration => ("迁移助手", $"待迁移 {_context.SelectedSoftware?.Count ?? 0} 个软件", "\uE8DE"),
            _ => ("AI 助手", "", "\uE99A")
        };

        ModuleTitle.Text = title;
        ModuleSubtitle.Text = subtitle;
        ModuleIcon.Glyph = icon;
        TitleIcon.Glyph = icon;
        TitleText.Text = title;
    }

    private void SetupQuickActions()
    {
        var actions = _aiAssistant.GetQuickActions(_context.Module);
        QuickActionsPanel.Children.Clear();

        foreach (var action in actions)
        {
            var btn = new Button
            {
                Tag = action,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            content.Children.Add(new FontIcon 
            { 
                Glyph = action.Icon, 
                FontSize = 16,
                Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            });
            content.Children.Add(new TextBlock { Text = action.Title, FontSize = 13 });
            
            btn.Content = content;
            btn.Click += QuickAction_Click;
            QuickActionsPanel.Children.Add(btn);
        }
    }

    #region Session Management

    private void CreateNewSession()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"新会话 {Sessions.Count + 1}",
            CreatedAt = DateTime.Now,
            Messages = new List<AIMessageItem>()
        };

        Sessions.Insert(0, session);
        _currentSession = session;
        
        // 选中新会话
        SessionListView.SelectedItem = session;
        
        Messages.Clear();
        AddWelcomeMessage();
    }

    private void RefreshSessionList()
    {
        // ObservableCollection 自动更新 UI，无需手动刷新
    }

    private void SwitchToSession(ChatSession session)
    {
        // 保存当前会话的消息
        if (_currentSession != null)
        {
            _currentSession.Messages = Messages.ToList();
        }

        _currentSession = session;
        Messages.Clear();

        // 加载会话消息
        foreach (var msg in session.Messages)
        {
            Messages.Add(msg);
        }

        // 如果是空会话，添加欢迎消息
        if (Messages.Count == 0)
        {
            AddWelcomeMessage();
        }
    }

    private void UpdateSessionTitle()
    {
        if (_currentSession != null && Messages.Count > 1)
        {
            // 使用第一条用户消息作为标题
            var firstUserMsg = Messages.FirstOrDefault(m => m.IsUser);
            if (firstUserMsg != null)
            {
                var title = firstUserMsg.Content;
                if (title.Length > 20)
                {
                    title = title.Substring(0, 20) + "...";
                }
                _currentSession.Title = title;
                // 触发 UI 更新
                var index = Sessions.IndexOf(_currentSession);
                if (index >= 0)
                {
                    Sessions[index] = _currentSession;
                }
            }
        }
    }

    #endregion

    #region Message Handling

    private void AddWelcomeMessage()
    {
        var welcomeText = _context.Module switch
        {
            AIModule.SoftwareList => "你好！我是软件分析助手。我可以帮你分析已安装的软件，识别可能不需要的程序，或者提供软件管理建议。\n\n你可以点击左下角的「快捷操作」选择常用功能，或直接输入你的需求。",
            AIModule.FileManager => "你好！我是文件整理助手。我可以帮你分析目录结构、整理文件、批量重命名等。\n\n你可以点击左下角的「快捷操作」选择常用功能，或直接输入你的需求。",
            AIModule.Cleanup => "你好！我是清理助手。我可以帮你分析磁盘空间、识别可清理的文件、清理临时文件等。\n\n你可以点击左下角的「快捷操作」选择常用功能，或直接输入你的需求。",
            AIModule.Migration => "你好！我是迁移助手。我可以帮你规划软件迁移、分析依赖关系、执行迁移操作等。\n\n你可以点击左下角的「快捷操作」选择常用功能，或直接输入你的需求。",
            _ => "你好！我是 AI 助手。请告诉我你需要什么帮助。"
        };
        Messages.Add(new AIMessageItem 
        { 
            IsUser = false, 
            Content = welcomeText, 
            MsgType = AIMsgType.Text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
    }

    private void AddUserMessage(string content)
    {
        Messages.Add(new AIMessageItem 
        { 
            IsUser = true, 
            Content = content, 
            MsgType = AIMsgType.Text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
        ScrollToBottom();
        UpdateSessionTitle();
    }

    private void AddAIMessage(string content)
    {
        Messages.Add(new AIMessageItem 
        { 
            IsUser = false, 
            Content = content, 
            MsgType = AIMsgType.Text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
        ScrollToBottom();
    }

    private void AddToolMessage(string content, bool success = true)
    {
        var prefix = success ? "[完成]" : "[失败]";
        Messages.Add(new AIMessageItem 
        { 
            IsUser = false, 
            Content = $"{prefix} {content}", 
            MsgType = AIMsgType.Tool,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
        ScrollToBottom();
    }

    private AIMessageItem AddLoadingMessage(string status)
    {
        var msg = new AIMessageItem 
        { 
            IsUser = false, 
            Content = status, 
            MsgType = AIMsgType.Loading,
            Timestamp = DateTime.Now.ToString("HH:mm")
        };
        Messages.Add(msg);
        ScrollToBottom();
        return msg;
    }

    private void RemoveLoadingMessages()
    {
        var toRemove = Messages.Where(m => m.MsgType == AIMsgType.Loading).ToList();
        foreach (var m in toRemove) Messages.Remove(m);
    }

    private void ScrollToBottom()
    {
        _dispatcherQueue.TryEnqueue(() => 
            ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null));
    }

    #endregion

    #region Event Handlers

    private bool _isSidebarCollapsed = false;
    private ChatSession? _contextMenuSession;

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        
        if (_isSidebarCollapsed)
        {
            // 收起侧边栏
            SidebarBorder.Visibility = Visibility.Collapsed;
            MiniSidebar.Visibility = Visibility.Visible;
            SidebarColumn.Width = new GridLength(48);
        }
        else
        {
            // 展开侧边栏
            SidebarBorder.Visibility = Visibility.Visible;
            MiniSidebar.Visibility = Visibility.Collapsed;
            SidebarColumn.Width = new GridLength(260);
        }
    }

    private void SessionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SessionListView.SelectedItem is ChatSession session && session != _currentSession)
        {
            SwitchToSession(session);
        }
    }

    private void SessionListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = e.OriginalSource as FrameworkElement;
        while (element != null && element.DataContext is not ChatSession)
        {
            element = element.Parent as FrameworkElement;
        }
        
        if (element?.DataContext is ChatSession session)
        {
            _contextMenuSession = session;
            var point = e.GetPosition(null);
            SessionContextMenu.HorizontalOffset = point.X;
            SessionContextMenu.VerticalOffset = point.Y;
            SessionContextMenu.IsOpen = true;
        }
    }

    private void DeleteSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ChatSession session)
        {
            DeleteSession(session);
        }
    }

    private void DeleteSessionFromMenu_Click(object sender, RoutedEventArgs e)
    {
        SessionContextMenu.IsOpen = false;
        if (_contextMenuSession != null)
        {
            DeleteSession(_contextMenuSession);
            _contextMenuSession = null;
        }
    }

    private void DeleteSession(ChatSession session)
    {
        var index = Sessions.IndexOf(session);
        Sessions.Remove(session);
        
        // 如果删除的是当前会话，切换到其他会话或创建新会话
        if (session == _currentSession)
        {
            if (Sessions.Count > 0)
            {
                var newIndex = Math.Min(index, Sessions.Count - 1);
                SessionListView.SelectedItem = Sessions[newIndex];
                SwitchToSession(Sessions[newIndex]);
            }
            else
            {
                CreateNewSession();
            }
        }
    }

    private void RenameSession_Click(object sender, RoutedEventArgs e)
    {
        SessionContextMenu.IsOpen = false;
        if (_contextMenuSession != null)
        {
            RenameTextBox.Text = _contextMenuSession.Title;
            
            // 显示重命名弹窗
            var transform = SessionListView.TransformToVisual(null);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            RenamePopup.HorizontalOffset = point.X + 20;
            RenamePopup.VerticalOffset = point.Y + 100;
            RenamePopup.IsOpen = true;
            
            RenameTextBox.Focus(FocusState.Programmatic);
            RenameTextBox.SelectAll();
        }
    }

    private void CancelRename_Click(object sender, RoutedEventArgs e)
    {
        RenamePopup.IsOpen = false;
        _contextMenuSession = null;
    }

    private void ConfirmRename_Click(object sender, RoutedEventArgs e)
    {
        RenamePopup.IsOpen = false;
        if (_contextMenuSession != null && !string.IsNullOrWhiteSpace(RenameTextBox.Text))
        {
            _contextMenuSession.Title = RenameTextBox.Text.Trim();
            // 触发 UI 更新
            var index = Sessions.IndexOf(_contextMenuSession);
            if (index >= 0)
            {
                Sessions[index] = _contextMenuSession;
            }
        }
        _contextMenuSession = null;
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        CreateNewSession();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Messages.Clear();
        AddWelcomeMessage();
        if (_currentSession != null)
        {
            _currentSession.Messages.Clear();
        }
    }

    private void QuickActionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var transform = element.TransformToVisual(null);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            
            QuickActionsPopup.HorizontalOffset = point.X;
            QuickActionsPopup.VerticalOffset = point.Y - 200;
            QuickActionsPopup.IsOpen = true;
        }
    }

    private void QuickAction_Click(object sender, RoutedEventArgs e)
    {
        QuickActionsPopup.IsOpen = false;
        
        if (sender is Button btn && btn.Tag is QuickAction action)
        {
            AddUserMessage(action.Title);
            _ = RunAgentAsync(action.Prompt);
        }
    }

    private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            SendMessage();
            e.Handled = true;
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e) => SendMessage();

    private void SendMessage()
    {
        var input = InputTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;

        InputTextBox.Text = string.Empty;
        AddUserMessage(input);
        _ = RunAgentAsync(input);
    }

    #endregion

    #region AI Agent

    private async Task RunAgentAsync(string userRequest)
    {
        IsInputEnabled = false;
        _cts = new CancellationTokenSource();

        try
        {
            await foreach (var evt in _aiAssistant.RunAsync(_context, userRequest, _cts.Token))
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    switch (evt.Type)
                    {
                        case AIAssistantEventType.Thinking:
                            if (Messages.LastOrDefault()?.MsgType != AIMsgType.Loading)
                                AddLoadingMessage(evt.Message ?? "正在分析...");
                            break;

                        case AIAssistantEventType.Message:
                            RemoveLoadingMessages();
                            if (!string.IsNullOrEmpty(evt.Message))
                                AddAIMessage(evt.Message);
                            break;

                        case AIAssistantEventType.ToolCall:
                            RemoveLoadingMessages();
                            AddToolMessage(evt.Message ?? $"执行: {evt.ToolName}");
                            break;

                        case AIAssistantEventType.ToolResult:
                            if (!string.IsNullOrEmpty(evt.ToolResult))
                                AddToolMessage(evt.ToolResult, evt.ToolSuccess ?? true);
                            break;

                        case AIAssistantEventType.Complete:
                            RemoveLoadingMessages();
                            if (!string.IsNullOrEmpty(evt.Message))
                                AddAIMessage(evt.Message);
                            break;

                        case AIAssistantEventType.Error:
                            RemoveLoadingMessages();
                            AddAIMessage($"[错误] {evt.Error?.Message}");
                            break;
                    }
                });
                await Task.Delay(30);
            }
        }
        catch (OperationCanceledException)
        {
            _dispatcherQueue.TryEnqueue(() => 
            { 
                RemoveLoadingMessages(); 
                AddAIMessage("操作已取消。"); 
            });
        }
        catch (Exception ex)
        {
            _dispatcherQueue.TryEnqueue(() => 
            { 
                RemoveLoadingMessages(); 
                AddAIMessage($"[错误] {ex.Message}"); 
            });
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsInputEnabled = true);
            _cts?.Dispose();
            _cts = null;
        }
    }

    #endregion

    #region Static Helpers for XAML Binding

    public static Brush GetAvatarBrush(bool isUser) => isUser
        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212))
        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 16));

    public static string GetAvatarGlyph(bool isUser) => isUser ? "\uE77B" : "\uE99A";

    public static string GetSenderLabel(bool isUser) => isUser ? "你" : "AI 助手";

    public static Brush GetMessageBackground(bool isUser) => isUser
        ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
        : (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];

    public static Visibility ShowIfText(AIMsgType type) => 
        type == AIMsgType.Text ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility ShowIfLoading(AIMsgType type) => 
        type == AIMsgType.Loading ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility ShowIfTool(AIMsgType type) => 
        type == AIMsgType.Tool ? Visibility.Visible : Visibility.Collapsed;

    #endregion

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

#region Data Models

public class AIMessageItem : INotifyPropertyChanged
{
    public bool IsUser { get; set; }
    
    private string _content = string.Empty;
    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }
    
    public AIMsgType MsgType { get; set; }
    
    public string Timestamp { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum AIMsgType { Text, Loading, Tool }

public class ChatSession
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<AIMessageItem> Messages { get; set; } = new();
}

#endregion
