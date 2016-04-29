# Config节点配置说明
**文件位置：** 此文件位于common项目下res文件夹下。

CIConfig文件用作数据库存储项目配置信息。包括svn全局配置信息，人员匹配信息 ，gitlab账号信息，以及每个项目的配置信息。

  - preferences    -全局配置信息
  - SlackPeople    -发送slack人名匹配
  - MailPeople     -发送mail人名匹配 
  - GitLabInfo     -gitlab账号存储（libgit2Sharp只支持http验证，不支持ssh）
  - Projects       -每个项目配置信息
  
### preferences
```parser3?linenums
 <preferences>
    <UpdateInterval></UpdateInterval>
    <SvnPath></SvnPath>
    <StandarOutput></StandarOutput>
    <ServiceSwitch></ServiceSwitch>
  </preferences>
```
| 子节点| 说明 |  示例
| -----|:----:| :----| 
| UpdateInterval   | 两次检查提交时间间隔 |1（min）
| SvnPath    | svn.exe在本机中的配置位置 |C:\Program Files\TortoiseSVN\bin\svn.exe
| StandarOutput   | 是否需要标准输出信息  |false或者true
| ServiceSwitch   | 选择运行编译的类型，window以及service|service或者window    

### SlackPeople
```parser3?linenums
  <SlackPeople>
    <People Name=""></People>
  </SlackPeople>
  ```
| 子节点| 说明 | 示例| 属性值 |属性示例
| -----|:----:| :----:| :----:| :----:| 
| People   | 名字全称 |xiaohua |Name 名字简写|xh

### MailPeople
```parser3?linenums
<MailPeople>
    <People Name=""></People>
  </MailPeople>
  ```
| 子节点| 说明 |示例|  属性值 |属性示例
| -----|:----:| :----:| :----:| :----:| 
| People   | 名字简写 |xh |Name 名字全称|xiaohua

### GitLabInfo
```parser3?linenums
<GitLabInfo>
    <Username></Username>
    <Password></Password>
    <Email></Email>
  </GitLabInfo>
  ```
| 子节点| 说明 |  示例
| -----|:----:| :----:| 
| Username   | gitlab账号名  |xiaohua
| Password   | gitlab密码|xiaohua_123
| Email   | gitlab关联邮箱|xiaohua@gmail.com

### Projects
```parser3?linenums
<Projects Name="" Status="">
  <RepositoryPath></RepositoryPath>
    <WorkingDirectory></WorkingDirectory>
    <MailTo></MailTo>
    <BuildCommand></BuildCommand>
    <IfSlack></IfSlack>
    <IfMail></IfMail>
    <SlackUrl></SlackUrl>
    <SlackChannel></SlackChannel>
    <MailHost></MailHost>
    <UserName></UserName>
    <Password></Password>
    <SlackUser></SlackUser>
    <SlackContent></SlackContent>
    <SlackUpdate></SlackUpdate>
    <SlackCommit></SlackCommit>
    <GitVersion></GitVersion>
    <ProjectType></ProjectType>
      </Projects>
  ```
| Projects节点| 属性1 |  属性1说明|属性1示例|属性2 |  属性2说明|属性2示例
| -----|:----:| :----:| :----:| :----:|  :----:| :----:| 
| Projects   | Name  |项目名称|demo|Status|bool类型，是否执行|false 或者 true


| 子节点| 说明 |  示例
| -----|:----:|  :----:| 
| RepositoryPath   | 远程库地址  | http://libgit2.github.com
| WorkingDirectory   | 本地工作目录|C:\demo
| MailTo   | 接受邮件的邮箱|yangxianghong@gmail.com
| BuildCommand   | 编译语句  |gradlew build android
| IfSlack   | 是否推送slack信息|false or true
| IfMail   | 是否发送邮件|false or true
| SlackUrl   | 推送slack的地址|https://hooks.slack.com/services/xxxxx
| SlackChannel   | slack频道|#demo
| MailHost   | 邮件服务器|smtp.gmail.com
| UserName   | 邮件发送人账户名|yangxiaohong
| Password   | 邮件发送人密码|yangxiaohong_123
| SlackUser   | slack用户 |yxh
| SlackContent   | slack推送信息模板|#smile:smiling_imp#projectName#revision versionNum#build successful,:build failed,#cost buildtime.#
| SlackUpdate   | 是否发送更新信息|true or false
| SlackCommit   | gitlab关联邮箱|true or false
| Version   | 版本号存储|355
| ProjectType   | 项目类型|svn or git


