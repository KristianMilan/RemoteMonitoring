# Remote Monitoring  [![GitHub](https://img.shields.io/github/license/capitoljay/remotemonitoring)](https://github.com/capitoljay/RemoteMonitoring/blob/master/LICENSE)
A remote monitoring solution for lightweight linux environments like the Raspberry Pi.

## Synopsis

Remote Monitoring strives to be an easy to use remote monitoring solution for the DIY enthusiast.  Currently free space and temperature through a DS19B20 one-wire device are supported on the Raspberry Pi, but the solution includes a robust set of core classes to support additional monitoring types in the future.

## Motivation

This project came about because I needed a relatively low power device I could put in a greenhouse to monitor the temperature.  Initially I created a prototype using an Arduino attached to a PC, but the Raspberr Pi was a much more elegant solution.  Now that I need remote monitoring in my life again, I decided to pull this project out of mothballs, and iterate on it once again.

## Installation

At present installation on a Raspberry Pi is a matter of copying the built code over to a Raspberry Pi with mono installed, switching the SQL provider to Mono.Data.Sqlite and running the executable.  From here you should be able to hit http://[YourIP]:8000 and see the interface (You can change this by altering the appSetting "ListenURL" in the app.config)

If you need to connect your DS18B20 sensors, you'll need to use this guide:  http://www.circuitbasics.com/raspberry-pi-ds18b20-temperature-sensor-tutorial/ to get them set up.

If you'd like to set the monitor up to use systemd, I've had success following this guide:  https://www.raspberrypi.org/documentation/linux/usage/systemd.md

## API Reference

Coming soon

## Contributors

Let people know how they can dive into the project, include important links to things like issue trackers, irc, twitter accounts if applicable.

## License

Remote Monitoring is under the [MIT License](LICENSE).

Remote Monitoring re-distributes other open-source tools and libraries. Please check the [third party licenses](REDISTRIBUTED.md).
