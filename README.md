***Only Chinese document is available.***

# EAutoBuild
扫描目录下所有 [TextECode](https://github.com/OpenEpl/TextECode) 生成的文本式易语言项目文件（`*.eproject`）并编排为 [Ninja](https://ninja-build.org/) 编译脚本的工具  

## 编译
```powershell
dotnet publish -c Release "-p:PublishProfile=Properties/PublishProfiles/SingleFile.pubxml" "-p:Version=0.0.1"
```

## 安装
需先 [安装 .NET 桌面运行时 3.1](https://dotnet.microsoft.com/zh-cn/download/dotnet/3.1) 才可使用本工具。  
本工具生成的 [Ninja](https://ninja-build.org/) 编译脚本需要使用 `Ninja` 执行，因此您应当正确安装 `Ninja` 并配置环境变量（或执行别名）。  
本工具生成的编译脚本将调用 `eplc` 命令，您需要在使用本工具前正确安装 `eplc` 并配置环境变量（或执行别名），且您应当使用支持输入文本代码的新版 `eplc` 。  
由于 `eplc` 处理文本代码时将自动调用 `TextECode`，您还需要正确安装 `TextECode` 并配置环境变量（或执行别名），使用 Appx 安装 `TextECode` 时通常会自动完成相关配置。  
为了方便您的使用，我们建议您将本程序也放入环境变量 `Path` 之中。  

#### 检验安装
在终端（推荐使用 [Windows Terminal](https://github.com/microsoft/terminal)）执行以下命令，应当均不报错：
```powershell
ninja --version
eplc --help
TextECode --version
```

## 使用
所有被本工具识别的 `*.eproject` 均应该包含 `OutFile` 属性以指定输出文件的相对路径，当存在模块引用关系时，系统会自动识别。  
命令行参数如下：
```
value pos. 0     Required. Set root directory
--script-name    (Default: build.ninja) Set the name of the generated ninja script
--include        Set filter to include files
--exclude        Set filter to exclude files
--build          (Default: false) Build instantly
--help           Display this help screen.
--version        Display version information.
```
示例：
- `EAutoBuild .`：扫描当前目录所有 `*.eproject` 并生成 `build.ninja`
- `EAutoBuild . --build`：扫描当前目录所有 `*.eproject` 、生成 `build.ninja` 并立即调用 [Ninja](https://ninja-build.org/) 进行编译
- `EAutoBuild . --exclude "Temp/**" --script-name "build_normal.ninja"`：扫描当前目录除 `Temp` 文件夹中文件以外的所有 `*.eproject` 并生成 `build_normal.ninja`

## 交流
一般的 bug 反馈 与 feature 请求，请用 GitHub 的 Issues 模块反馈  
如果您希望对本项目做出贡献，请使用标准 GitHub 工作流：Fork + Pull request  
进一步的快速讨论：请加入 QQ 群 `605310933` 或 QQ 频道 `e81tgd8w3m` *（注意不要在群中反馈 bug，这很可能导致反馈没有被记录。聊天消息较 Issues 模块比较混乱）*  

## 许可
本项目使用 [MIT License](./LICENSE.txt) 许可证