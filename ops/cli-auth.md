# CLI Auth Bootstrap

## GitHub

`gh auth status`

Expected scopes: `repo`, `workflow`, package write via `GITHUB_TOKEN` in actions.

## Linode

Set `LINODE_TOKEN` then:

`linode-cli linodes list`

## Cloudflare

Set `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ZONE_ID`, then:

`curl -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" https://api.cloudflare.com/client/v4/zones/$CLOUDFLARE_ZONE_ID/dns_records`
