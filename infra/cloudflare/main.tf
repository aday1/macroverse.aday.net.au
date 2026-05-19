terraform {
  required_version = ">= 1.5.0"
  required_providers {
    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = ">= 4.0.0"
    }
  }
}

provider "cloudflare" {
  api_token = var.cloudflare_api_token
}

variable "cloudflare_api_token" {
  type      = string
  sensitive = true
}

variable "zone_id" { type = string }
variable "linode_ipv4" { type = string }

resource "cloudflare_record" "macroverse_live" {
  zone_id = var.zone_id
  name    = "macroverse"
  type    = "A"
  content = var.linode_ipv4
  proxied = true
  ttl     = 1
}

resource "cloudflare_record" "macroverse_test" {
  zone_id = var.zone_id
  name    = "test.macroverse"
  type    = "A"
  content = var.linode_ipv4
  proxied = true
  ttl     = 1
}

resource "cloudflare_record" "macroverse_aday" {
  zone_id = var.zone_id
  name    = "aday.macroverse"
  type    = "A"
  content = var.linode_ipv4
  proxied = true
  ttl     = 1
}

resource "cloudflare_record" "artbastard_live" {
  zone_id = var.zone_id
  name    = "artbastard"
  type    = "A"
  content = var.linode_ipv4
  proxied = true
  ttl     = 1
}

resource "cloudflare_record" "artbastard_test" {
  zone_id = var.zone_id
  name    = "test.artbastard"
  type    = "A"
  content = var.linode_ipv4
  proxied = true
  ttl     = 1
}
