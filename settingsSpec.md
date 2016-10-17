# 設定ファイルの仕様
## 設定ファイルの生成について
C#の機能（プロジェクト→プロパティ→設定）を使っているため  
外部アプリケーションで設定を自動生成する場合は同様に  
「設定ファイル」の設定をC#することで、設定ファイル（XML）の自動生成が可能  
### BaudRate：ボーレートの設定  
* intの数値の範囲で設定  

### DataBits：データ（1バイト）のビット幅（機器固有の値）  
* 5～8ビットで設定（C#の仕様上の制限）  

### Parity：パリティの種類
設定|意味|
:----|----  
none|なし|
even|偶数パリティチェック|
mark|パリティビットを常に1に設定|
odd|奇数パリティチェック|
space|パリティビットを常に0に設定|

### StopBits:ストップビットの種類
設定|意味
:---|---
1|ストップビットが1ビット
1.5|ストップビットが1.5ビット
2|ストップビットが2ビット

### FlowControl:フロー制御
設定|意味
:---|---
none|フロー制御しない
rts|ハードウェアフロー制御を有効にする
rtsXonXoff|ソフトウェアおよびハードウェアフロー制御を有効にする
XonXoff|ソフトウェアフロー制御を有効にする

設定ファイルの一部抜粋  
以下の適切な位置に上記の設定を入れることで、様々な機器に対応できる
```
<rs232c.Properties.Settings>
    <setting name="BaudRate" serializeAs="String">
        <value>9600</value>
    </setting>
    <setting name="DataBits" serializeAs="String">
        <value>8</value>
    </setting>
    <setting name="Parity" serializeAs="String">
        <value>none</value>
    </setting>
    <setting name="StopBits" serializeAs="String">
        <value>1</value>
    </setting>
    <setting name="FlowControl" serializeAs="String">
        <value>none</value>
    </setting>
</rs232c.Properties.Settings>
```