
output "function_app_name" {
  value = azurerm_linux_function_app.function_app.name
  description = "Deployed function app name"
}

output "function_app_default_hostname" {
  value = azurerm_linux_function_app.function_app.default_hostname
  description = "Deployed function app hostname"
}

output "function_app_service_bus_namespace" {
  value = azurerm_servicebus_namespace.service_bus.name
  description = "Deployed service bus namespace"
}