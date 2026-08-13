# 配布物の署名

公開用のセットアップと配布EXEは、ローカルの Mirin コード署名ポリシーに従って、生成後に署名します。

- 署名者: `CN=Mirin`
- ダイジェスト: SHA-256
- タイムスタンプ: RFC 3161 (`http://timestamp.digicert.com`)
- 証明書: `Cert:\CurrentUser\My` にある有効期限内のコード署名証明書

秘密鍵、PFX、DPAPI パスワード、署名ログはリポジトリへ保存しません。署名環境がない場合は、ビルドしたファイルを公開用として扱わず、署名済みファイルだけをReleaseへ添付します。

例:

```powershell
$cert = Get-ChildItem 'Cert:\CurrentUser\My\<thumbprint>'
Set-AuthenticodeSignature -FilePath .\artifacts\publish\PdfToolbox.exe `
  -Certificate $cert -HashAlgorithm SHA256 `
  -TimestampServer 'http://timestamp.digicert.com'
```
