**LuckyCI**
===================

LuckyCI是运行在Windows平台上的持续集成工具(CI)。可以对gitlab版本库上的gradle项目，如Android Studio、 Apache Cordova,以及 .NET项目进行监控并持续集成（Build）。集成结果可以通过**邮件**或**Slack**自动发送给团队成员，从而时刻确保项目完整性，减少由于错误提交产生的排错内耗，让开发过程更加高效。

功能简介
-------------

> - LuckyCI提供**Windows客户端**和**Windows Service**两种运行模式,**Windows客户端**提供了GUI界面对运行环境和项目进行配置，测试，在前台运行CI服务； **Windows Service**以Windows服务的形式运行CI服务
> - 用户可以在**Windows客户端**查看操作执行的详细结果,项目运行的日志信息以及**gitlab**集成功能执行的日志信息。
> - 集成消息实时推送功能, 在服务器端配置推送项目实时运行情况 (目前支持**邮件**和**slack**推送)。

项目架构情况
-------------
> - 开发者提交代码（包括分支），gitlab会推送消息到LuckyCIPost。
> - LuckyCIPost将接收到的消息发送给RabbitMQ进行队列排序。
> - LuckyCIService接收RabbitMQ的队列数据，一条一条的进行处理。
> - LuckyCIService将数据处理完成后，将处理结果推送至email以及slack。

CI项目目录管理
-------------
#####coommon
类库，封装了整个项目的类，其他目录从此目录进行方法的调用。
#####LuckyCI
Windows客户端模式，可以用来添加项目，监控并且操控服务的运行状态，查看项目的编译情况等。
#####LuckyCIPost
接收gitlab的推送消息，并将消息发送到[rabbitMQ](http://www.rabbitmq.com/)中。
#####LuckyCIService
接收rabbitMQ的消息，对消息进行处理，然后对项目进行编译，编译消息进行推送。

管理项目
-------------
LuckyCI中已经为用户添加了三个项目样例(.Net , Cordova , AndroidStudio),以便用户参考使用,样本样例无法进行正常git操作及项目操作,仅供参考学习。

####<i class="icon-pencil"></i>邮件与Slack的配置

如果用户想及时收到消息的推送以便监控项目的情况,可以配置邮件或Slack。
> - 邮件需要用户修改默认域名,输入相应服务器地址,若不更改用户名LuckyCI将会使用git的对应项目提交Author作为默认此时用户无需输入密码。
> - Slack则需要用户在Slack提供的Incoming WebHooks页面找到团队的Webhook URL,同时在页面也可以对消息机器人进行简单配置。同样如果不设置用户名我们将默认使用git的Author

#### <i class="icon-pencil"></i>修改项目

点击<kbd>File</kbd>进入项目监测页面,在当前页用户可以对Service的状态进行查看及操作,查看配置好的项目的历史日志信息,单击(或双击)选中项目列表中的项目进入修改模式,修改完后点击<kbd>Save</kbd>进行保存。

#### <i class="icon-pencil"></i> 帮助文档

点击<kbd>Help</kbd>查看帮助文档,解决一些在AndroidStudio出现的一些常见问题。


   