﻿**LuckyCIPost**
---------------
LuckyCIPost是用iis为承载的支持gitlab的webhook。当代码提交时（不管哪个分支），gitlab都会
push一条json数据到LuckyCIPost进行处理。
***配置方式***
----------------
> - 首先将LuckyCIPost用iis发布。[具体iis搭建详解](http://www.cr173.com/html/75973_1.html)。
> - 打开gitlab，在相应的项目添加webhook地址，根据自己的电脑ip添加地址，
格式："ip地址:分配的端口/api/PostInfo/PostInfoFunction"。
    gitlab中添加webhook步骤：Settings->Webhooks，在打开的页面中添加上URL。