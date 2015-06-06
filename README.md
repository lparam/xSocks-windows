Xsocks for Windows
=======================
A [xsocks](https://github.com/lparam/xsocks) client for Windows

#### Features

1. System proxy configuration
2. Fast profile switching
3. PAC mode and global mode
4. GFWList and user rules
5. Supports HTTP proxy

#### Usage

1. Find Xsocks icon in the notification tray
2. You can add multiple servers in servers menu
3. Select Enable System Proxy menu to enable system proxy. Please disable other
proxy addons in your browser, or set them to use system proxy
4. You can also configure your browser proxy manually if you don't want to enable
system proxy. Set Socks5 or HTTP proxy to address of tray icon tips. You can change this
port in Server -> Edit Servers
5. You can change PAC rules by editing the PAC file. When you save the PAC file
with any editor, Xsocks will notify browsers about the change automatically
6. You can also update the PAC file from GFWList. Note your modifications to the PAC
file will be lost. However you can put your rules in the user rule file for GFWList.
Don't forget to update from GFWList again after you've edited the user rule
7. Your system may need [Visual C++ Redistributable Packages](https://www.microsoft.com/en-US/download/details.aspx?id=40784)

### Develop

Visual Studio Express 2013 is recommended.

#### License

GPLv3
