# luckyci
The SVN CI tool for Android Studio/.NET/Apache Cordova projects. Notify user with Email and Slack 
   At luckyci,you will receive an email or slack infomation about building log of every revision that you upload.Of course that,
if you want not to  receive email or slack,you can set "false" about status.
   You must konw that there are two models to config email of the sender infomation,you must choose one and just one.About first model,you can
see a demo at UI,you should modify "domain" part.As for the author,default author is people that lastest uploading.Other model,
you sould set email of username and password to send email.
   At main window,you can see all status about projects and build log infomation.Different colors has different means about status.
And about luckyci,we have two models to run--window and windows service.you can config one.About window,there is a interface,you 
can do what you want do about projects,but not closing.Other model,it is "windows service".It can auto build projects that you 
update,and send email to the recipient.Also that,if the interface is open,you can monitor status about windows service and can  
control it,such as "on","of","reset".
