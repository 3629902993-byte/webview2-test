# WebView2性能测试工具 Readme.md
```markdown
# WebView2 性能测试套件
> VB.NET (.NET8 WinForms) + WebView2 测试浏览器 + 性能测试网页
> 用途：测试 Windows11 系统自带 WebView2 内核加载、JS运算、DOM渲染性能

## 环境要求
1. 操作系统：Windows 11（自带WebView2 Runtime，无需单独安装Edge运行时）
2. 开发环境：Visual Studio 2022
3. .NET版本：.NET 8
4. NuGet包：`Microsoft.Web.WebView2`（只安装这一个包）

## 项目组成
- WebViewTest.vb：VB.NET 浏览器主程序（纯代码，**不需要窗体设计器拖拽控件**）
- test.html：Web性能测试页面（部署在Web服务器，用于压测WebView2）
- README.md：本说明文档

## 部署步骤
### 1. VB测试浏览器
1. VS2022新建项目：Windows窗体应用（Visual Basic），框架选择.NET 8
2. NuGet管理器安装包：`Microsoft.Web.WebView2`
3. 删除默认Form里面所有设计器控件，直接替换Form1.vb完整代码
4. 清理解决方案 → 重新生成
> ⚠️ 注意：代码使用 `Microsoft.Web.WebView2.WinForms.WebView2`，**不要引入WPF版本WebView2**，否则会报`Navigate`成员缺失、WindowsBase版本冲突。

### 2. 测试网页服务（test.html）
二选一启动web服务：
- 方式A（Python简易服务器，推荐快速测试）
  在test.html所在目录打开CMD：
  ```bash
  python -m http.server 8080
  ```
  访问地址：`[http://127.0.0.1:8080/test.html](http://127.0.0.1:8080/test.html)`

- 方式B：IIS / Nginx 部署
  将test.html放置网站根目录，通过内网/公网地址访问。

### 3. 修改浏览器默认访问地址
在VB代码内找到这一行，修改为你的网页地址：
```vb
Dim mySite As String = "[http://127.0.0.1:8080/test.html](http://127.0.0.1:8080/test.html)"
```

## 功能说明
### VB浏览器功能
1. 地址栏、前进、后退、刷新按钮
2. 自动计时：页面从开始导航到加载完成耗时（窗口标题显示毫秒）
3. 内置F12开发者工具（WebView DevTools，可查看页面Performance、Memory）
4. 纯代码动态创建控件，不依赖窗体设计器

### test.html页面功能
1. JS数学循环计算（模拟前端CPU运算压力）
2. 动态批量生成DOM元素，测试DOM渲染性能
3. CSS渐变样式渲染
4. 页面内自动输出渲染耗时，控制台打印日志
5. 无外部资源依赖，全部内置，不受外网速度影响

## 性能测试操作步骤
1. 启动Web服务器，确认浏览器直接打开网址能正常访问test.html
2. 运行VB WebView2测试程序，自动加载测试页面
3. 观察：
   - 窗体标题：页面加载总耗时（ms）
   - 页面内：网页自身JS统计渲染耗时
   - F12开发者工具 → Performance：录制页面加载、JS执行、渲染
   - Windows任务管理器：查看本程序 + WebView2子进程内存占用
4. 多次测试取平均值，减少单次波动误差

## 已知坑 & 排查
1. 报错 `Navigate 不是WebView2的成员`
   ✅ 原因：引用了WPF版WebView2。确认Imports是 `Microsoft.Web.WebView2.WinForms`，删掉WPF相关引用。
2. WindowsBase版本冲突
   ✅ NuGet不要手动添加dll引用，不要导入Wpf命名空间。新建干净.NET8项目重装NuGet包。
3. WebView初始化失败
   ✅ Win11自带Runtime；Win10需要手动安装WebView2 Runtime。
4. 网页打不开
   ✅ 确认Web服务正常，地址不要写错；防火墙不要拦截本地端口。

## 可选扩展
1. 命令行参数模式：外部EXE启动浏览器，自动传入测试URL
2. 加重版test.html：增加动画、大量DOM、图片压测内存
3. 结果导出：把加载耗时写入日志txt文件，批量记录性能数据

## 授权
仅用于本地性能测试，自由修改。
