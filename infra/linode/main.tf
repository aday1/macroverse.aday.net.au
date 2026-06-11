terraform {
  required_version = ">= 1.5.0"
  required_providers {
    linode = {
      source  = "linode/linode"
      version = ">= 2.0.0"
    }
  }
}

provider "linode" {
  token = var.linode_token
}

variable "linode_token" {
  type      = string
  sensitive = true
}

variable "label" { type = string  default = "aday-web-stack" }
variable "region" { type = string default = "ap-southeast" }
variable "type" { type = string default = "g6-nanode-1" }
variable "authorized_keys" { type = list(string) default = [] }
variable "root_pass" { type = string sensitive = true }

resource "linode_instance" "web" {
  label           = var.label
  image           = "linode/debian12"
  region          = var.region
  type            = var.type
  authorized_keys = var.authorized_keys
  root_pass       = var.root_pass
}
