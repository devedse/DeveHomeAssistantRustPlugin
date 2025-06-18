# Rust Smart Components Integration for Home Assistant

A Home Assistant custom integration that connects your Rust game smart components to your smart home ecosystem through the Rust+ mobile app API.

## What This Plugin Does

This integration automatically discovers and manages Rust game smart components in your Home Assistant setup, allowing you to:

- **Monitor storage containers** (Tool Cupboards, Large Storage Boxes, Vending Machines) 
- **Control smart switches** remotely from Home Assistant
- **Receive smart alarm notifications** directly in Home Assistant
- **Automate responses** to in-game events using Home Assistant automations
- **Create dashboards** showing your base status, storage levels, and security alerts

## Supported Smart Components

### Storage Monitor
- **Function**: Monitors inventory changes in storage containers
- **Home Assistant Entity**: Binary sensor that triggers when storage state changes
- **Use Cases**: Get notified when someone accesses your storage, automate lights when storage is accessed, track base activity

### Smart Alarm  
- **Function**: Sends customizable notifications when triggered
- **Home Assistant Entity**: Binary sensor + notification service
- **Use Cases**: Security alerts, raid notifications, base breach warnings

### Smart Switch
- **Function**: Remote controllable electrical switch
- **Home Assistant Entity**: Switch entity you can toggle on/off
- **Use Cases**: Remote base lighting, trap activation, electrical system control

## How It Works

1. **Rust+ App Integration**: The plugin connects to the same Rust+ API that the mobile app uses
2. **Automatic Discovery**: When you pair smart components in-game, they automatically appear in Home Assistant
3. **Real-time Updates**: Component states are updated in real-time as things happen in-game
4. **Two-way Control**: Control compatible components (like Smart Switches) directly from Home Assistant

## Installation

### Prerequisites
- Home Assistant Core 2024.1.0 or newer
- Rust+ mobile app installed and configured
- At least one Rust server with Rust+ enabled
- Smart components placed and paired in-game

### HACS Installation (Recommended)
1. Open HACS in your Home Assistant instance
2. Click on "Integrations"
3. Click the "+" button and search for "Rust"
4. Install the "Rust Smart Components" integration
5. Restart Home Assistant

### Manual Installation
1. Download the latest release from the [releases page](https://github.com/yourusername/DeveHomeAssistantRustPlugin/releases)
2. Extract the `rust` folder to your `custom_components` directory
3. Restart Home Assistant

## Configuration

### Initial Setup
1. Go to Settings → Devices & Services
2. Click "Add Integration"
3. Search for "Rust" and select it
4. Follow the configuration flow:
   - Enter your Steam credentials (encrypted and stored securely)
   - Select which Rust servers to monitor
   - The integration will automatically discover paired smart components

### Pairing Smart Components In-Game

1. **Install Rust+ App** on your mobile device
2. **Join a Rust+ enabled server**
3. **Craft smart components** (Storage Monitor, Smart Alarm, Smart Switch)
4. **Place and power** the components in your base
5. **Pair with Rust+ app**:
   - Hold E on the component
   - Select "Pair" 
   - Component appears in Rust+ app
6. **Restart Home Assistant integration** - components will be automatically discovered

## Usage Examples

### Basic Monitoring
- View storage status on your Home Assistant dashboard
- Get mobile notifications when storage is accessed
- Monitor base power consumption through smart switches

### Automation Ideas
```yaml
# Turn on exterior lights when storage is accessed at night
automation:
  - alias: "Base Security Lighting"
    trigger:
      - platform: state
        entity_id: binary_sensor.rust_storage_monitor_main_loot
        to: 'on'
    condition:
      - condition: sun
        after: sunset
    action:
      - service: switch.turn_on
        entity_id: switch.rust_smart_switch_exterior_lights

# Send notification when smart alarm triggers
automation:
  - alias: "Base Raid Alert"
    trigger:
      - platform: state
        entity_id: binary_sensor.rust_smart_alarm_entrance
        to: 'on'
    action:
      - service: notify.mobile_app_your_phone
        data:
          title: "🚨 BASE UNDER ATTACK!"
          message: "Smart alarm triggered at {{ trigger.from_state.attributes.location }}"
          data:
            priority: high
```

### Dashboard Cards
Create beautiful dashboard cards showing:
- Storage fill levels and recent activity
- Base security status with alarm states  
- Quick controls for lighting and electrical systems
- Server population and wipe cycle information

## Entity Naming Convention

Entities are automatically named based on your in-game setup:
- `binary_sensor.rust_storage_monitor_[location]`
- `binary_sensor.rust_smart_alarm_[custom_name]` 
- `switch.rust_smart_switch_[description]`

You can customize these names in Home Assistant's entity registry.

## Troubleshooting

### Components Not Appearing
- Ensure components are properly paired with Rust+ mobile app first
- Check that the Rust server has Rust+ enabled
- Verify your Steam credentials in the integration config
- Try restarting the Home Assistant integration

### Connection Issues  
- Confirm your internet connection is stable
- Check that Rust+ mobile app is working properly
- Verify Steam account has access to the Rust servers
- Review Home Assistant logs for specific error messages

### Component State Not Updating
- Ensure components are powered in-game
- Check electrical connections and power generation
- Verify components haven't been damaged or destroyed
- Component must have TC (Tool Cupboard) authorization

## API and Development

This integration uses the Rust+ API protocol to communicate with Facepunch's servers. The API is reverse-engineered from the mobile app and provides:

- Real-time component state monitoring
- Component control capabilities  
- Server information and player data
- Secure authentication through Steam

### Component Technical Details

| Component | Power Usage | Update Frequency | Control Type |
|-----------|-------------|------------------|--------------|
| Storage Monitor | 1rW | On inventory change | Read-only |
| Smart Alarm | 1rW | On trigger | Read-only + Notifications |
| Smart Switch | 1rW | Real-time | Read/Write |

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) and check out our [Code of Conduct](CODE_OF_CONDUCT.md).

### Development Setup
1. Clone this repository
2. Set up a Home Assistant development environment
3. Install development dependencies: `pip install -r requirements_dev.txt`
4. Run tests: `pytest`

## Support

- **Issues**: Report bugs and feature requests on [GitHub Issues](https://github.com/yourusername/DeveHomeAssistantRustPlugin/issues)
- **Discussions**: Join the conversation in [GitHub Discussions](https://github.com/yourusername/DeveHomeAssistantRustPlugin/discussions)
- **Discord**: Join our community Discord server [link]

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

This integration is not affiliated with Facepunch Studios or the official Rust game. It's a community-created tool that uses the public Rust+ API. Use at your own risk and ensure compliance with game terms of service.

---

**Made with ❤️ by the Home Assistant community**

*Survive, build, automate* 🏠🔧⚙️
