variable "agent_count" {
  default = 2
}

# The following two variable declarations are placeholder references.
# Set the values for these variable in terraform.tfvars
variable "aks_service_principal_app_id" {
  default = ""
}

variable "aks_service_principal_client_secret" {
  default = ""
}

variable "cluster_name" {
  default = "AKSMonitorOpenTelemetry"
}

variable "dns_prefix" {
  default = "learnk8scluster"
}

# Refer to https://azure.microsoft.com/global-infrastructure/services/?products=monitor for available Log Analytics regions.
variable "log_analytics_workspace_location" {
  default = "KoreaCentral"
}

variable "log_analytics_workspace_name" {
  default = "testLogAnalyticsWorkspaceName"
}

# Refer to https://azure.microsoft.com/pricing/details/monitor/ for Log Analytics pricing
variable "log_analytics_workspace_sku" {
  default = "PerGB2018"
}

variable "resource_group_location" {
  default     = "KoreaCentral"
  description = "Location of the resource group."
}

variable "resource_group_name" {
  default     = "MonitorOpenTelemetry"
  description = "resource group name"
}


variable "ssh_public_key" {
  default = "~/.ssh/id_rsa.pub"
}
