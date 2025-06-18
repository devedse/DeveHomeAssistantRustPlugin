# Rust Home Assistant Bridge

A bridge service that connects Rust+ game servers to Home Assistant, allowing you to monitor your Rust server and smart devices from your home automation system.

## Features

- **Real-time Server Monitoring**: Get server info, player count, and game time
- **Smart Device Integration**: Monitor and control smart switches, storage monitors
- **Team Chat Integration**: Send and receive team chat messages
- **Map Information**: Access server map data and team member locations
- **Home Assistant Webhooks**: Automatically send data to Home Assistant via webhooks
- **RESTful API**: Control and query the bridge via HTTP endpoints

## Prerequisites

- .NET 9.0 or later
- Home Assistant instance with webhook integration
- Rust+ credentials (Server IP, Port, Player ID, Player Token)

## Getting Rust+ Credentials

To use this bridge, you need Rust+ credentials. Currently, the easiest way to get these credentials is using [this project](https://github.com/liamcottle/rustplus.js).

You'll need:
- **Server**: IP address of your Rust server
- **Port**: Rust+ companion app port (not the game port)
- **Player ID**: Your Steam ID
- **Player Token**: FCM player token
- **Use Facepunch Proxy**: Whether to use Facepunch's proxy (usually false)

## Configuration

### 1. Update `appsettings.json`

```json
{
  "RustPlus": {
    "Server": "your.rust.server.ip",
    "Port": 28082,
    "PlayerId": 76561198123456789,
    "PlayerToken": "your-player-token-here",
    "UseFacepunchProxy": false
  },
  "HomeAssistant": {
    "BaseUrl": "http://your-homeassistant:8123",
    "AccessToken": "your-long-lived-access-token",
    "WebhookId": "your-webhook-id"
  }
}
```

### 2. Set up Home Assistant Webhook

In Home Assistant, create a webhook automation:

1. Go to **Settings** → **Automations & Scenes**
2. Create a new automation
3. Set trigger type to **Webhook** and note the webhook ID
4. Use the webhook ID in your configuration

Example Home Assistant automation:

```yaml
alias: "Rust Server Events"
description: "Handle events from Rust server"
trigger:
  - platform: webhook
    webhook_id: your-webhook-id
action:
  - service: notify.persistent_notification
    data:
      title: "Rust Server Update"
      message: "{{ trigger.json.type }}: {{ trigger.json.data }}"
```

## API Endpoints

The bridge exposes several REST API endpoints:

### Server Information
- `GET /api/rustplus/server-info` - Get server information

### Entity Management
- `GET /api/rustplus/entity/{entityId}` - Get general entity info
- `GET /api/rustplus/smart-switch/{entityId}` - Get smart switch specific info

### Team Features
- `POST /api/rustplus/team-message` - Send team message
  ```json
  {
    "message": "Hello from Home Assistant!"
  }
  ```
- `GET /api/rustplus/team` - Get team information

### Map Data
- `GET /api/rustplus/map` - Get map information

## Webhook Events

The bridge sends the following webhook events to Home Assistant:

### Server Information (`rust_server_info`)
```json
{
  "type": "rust_server_info",
  "data": {
    "server_name": "My Rust Server",
    "player_count": 15,
    "max_players": 100,
    "timestamp": 1703123456
  }
}
```

### Smart Switch (`rust_smart_switch`)
```json
{
  "type": "rust_smart_switch",
  "data": {
    "entity_type": "smart_switch",
    "is_active": true,
    "timestamp": 1703123456
  }
}
```

### Storage Monitor (`rust_storage_monitor`)
```json
{
  "type": "rust_storage_monitor",
  "data": {
    "entity_type": "storage_monitor",
    "has_items": true,
    "item_count": 25,
    "timestamp": 1703123456
  }
}
```

### Team Chat (`rust_team_chat`)
```json
{
  "type": "rust_team_chat",
  "data": {
    "entity_type": "team_chat",
    "player_name": "PlayerName",
    "message": "Hello team!",
    "timestamp": 1703123456
  }
}
```

### Generic Entity Update (`rust_entity_update`)
```json
{
  "type": "rust_entity_update",
  "data": {
    "entity_type": "generic_entity",
    "entity_id": 123456789,
    "timestamp": 1703123456
  }
}
```

## Running the Bridge

### Development
```bash
dotnet run
```

### Production
```bash
dotnet publish -c Release
cd bin/Release/net9.0/publish
dotnet RustHomeAssistantBridge.dll
```

### Docker
The project includes Docker support. You can build and run using Docker:

```bash
docker build -t rust-ha-bridge .
docker run -p 8080:8080 rust-ha-bridge
```

## Monitoring and Logs

The bridge provides detailed logging at different levels:

- **Information**: Connection status, successful operations
- **Warning**: Failed operations, connection issues
- **Error**: Critical errors, exceptions
- **Debug**: Detailed operation information (development only)

## Home Assistant Integration Examples

### Creating Sensors

Create sensors in Home Assistant to track your Rust server:

```yaml
# configuration.yaml
sensor:
  - platform: webhook
    name: "Rust Server Player Count"
    webhook_id: your-webhook-id
    value_template: "{{ value_json.data.player_count }}"
    
  - platform: webhook
    name: "Rust Server Name"
    webhook_id: your-webhook-id
    value_template: "{{ value_json.data.server_name }}"
```

### Creating Automations

Send notifications when certain events occur:

```yaml
automation:
  - alias: "Rust Server Full"
    trigger:
      platform: webhook
      webhook_id: your-webhook-id
    condition:
      condition: template
      value_template: "{{ trigger.json.data.player_count >= trigger.json.data.max_players }}"
    action:
      service: notify.mobile_app_your_phone
      data:
        title: "Rust Server Full!"
        message: "Your Rust server is at capacity with {{ trigger.json.data.player_count }} players"
```

## Troubleshooting

### Common Issues

1. **Connection Refused**: Check your Rust+ credentials and server status
2. **Webhook Not Received**: Verify Home Assistant webhook configuration
3. **Authentication Error**: Ensure your Home Assistant access token is valid

### Debugging

Enable debug logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "RustHomeAssistantBridge": "Debug"
    }
  }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Credits

This project uses the [RustPlusApi](https://github.com/HandyS11/RustPlusApi) library by HandyS11 for Rust+ server communication.

## License

This project is open source. Please check the license file for details.
