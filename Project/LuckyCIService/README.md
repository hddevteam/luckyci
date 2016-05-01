**LuckyCIService**
--------------------
LuckyCIService是对提交代码的项目进行编译，并且将编译结果推送至email以及slack。
LuckyCIService的数据来自于RabbitMQ中的队列数据。
***LuckyCIService的配置***
LuckyCIService是作为一个windows server在运行，实时接收rabbitMQ的数据。
> - 首先找到LuckyCIService.exe的位置目录，在桌面创建一个.bat文件
（名字随便取，后缀必须是.bat）,编辑文件“cd C:\Windows\Microsoft.NET\Framework\v4.0.30319 
InstallUtil.exe + LuckyCIService.exe的位置   pause”，以管理员身份运行本文件，显示安装完成。
> - 开启LuckyCIService。步骤：右键我的电脑->管理->服务和应用程序->服务,找到LuckyCIServiceGit_RabbitMQ
，双击，启动此服务。