# Wake-on-LAN Windows Tool Implementation Plan

## Project Goal

Build a Windows desktop tool for internal company use that can:

- scan devices in the current LAN subnet
- collect host name, IP address, and MAC address for online devices
- save selected devices into a local host list
- wake a selected device from the saved host list by sending a Wake-on-LAN magic packet

This tool should be:

- lightweight
- stable
- simple to use

## Assumptions

- all target devices are already configured correctly for Wake-on-LAN
- all devices are in the same LAN subnet
- this is an internal tool, not a commercial product
- users should not need to understand network details

## Technical Choices

- Language: C#
- UI: WinForms
- Runtime: .NET
- Local storage: JSON file
- Network implementation:
  - UDP socket for Wake-on-LAN
  - ping scan for online devices
  - ARP lookup to obtain MAC addresses

## Why This Stack

- WinForms is lightweight and mature on Windows
- C# is stable and practical for Windows desktop and networking work
- JSON storage is simple and enough for the current scope
- this combination keeps startup fast, memory usage low, and maintenance easy

## MVP Scope

- scan online devices in the current LAN subnet
- read host name, IP, and MAC when available
- add scanned devices into a saved host list
- manually add, edit, and delete host records
- wake a selected host from the host list
- persist the host list locally
- hide advanced network configuration from normal users

## Out of Scope for V1

- cross-subnet scanning
- multi-NIC selection
- custom UDP port configuration
- custom broadcast address configuration
- batch wake
- tray mode
- vendor lookup by MAC prefix
- permission system
- server-side deployment

## UI Structure

The app should have only two tabs.

### 1. Host List

Display fields:

- Name
- Host name or remark
- MAC address

Actions:

- Wake
- Edit
- Delete
- Add manually

### 2. Search Devices

Display:

- one "Start Scan" button
- one result list

Result fields:

- Host name
- IP address
- MAC address

Actions:

- Add selected device to host list

## Main User Flow

1. Open the app and enter the Host List tab.
2. Open the Search Devices tab and scan the current LAN subnet.
3. Select a discovered device and add it to the host list.
4. Later, wake the device directly from the host list.

## Implementation Notes

### Startup and Storage

- load the local JSON file on startup
- save host list changes after add, edit, or delete operations

### Device Discovery

- detect the active local network adapter automatically
- determine the current subnet from the active adapter
- scan the subnet for online devices
- use ARP data to map IP to MAC
- try to resolve host names when possible

Important limitation:

- scanning is only for discovering currently online devices
- it does not guarantee that every device in the subnet will be found
- it is not intended to discover sleeping devices

### Wake-on-LAN

- generate a magic packet from the saved MAC address
- send the packet by UDP to the current subnet broadcast address
- the UI should expose only a simple "Wake" action

## Suggested Local Data Model

Each saved host record should include:

- Id
- Name
- HostName
- MacAddress
- LastKnownIp
- Remark
- LastSeenAt
- LastWakeAt

Required field for wake:

- MacAddress

## Development Direction

Implement the MVP directly from this file.

Priorities:

- keep the app lightweight
- keep the app stable
- keep the UI simple
- keep the code easy to maintain

Default behavior should hide unnecessary technical details and support the shortest user path:

- scan devices
- add to list
- click wake
