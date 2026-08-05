# Meteion WPF Toolkit
This toolkit is created for WPF developers like us to help them rapidly build WPF applications by reducing boilerplate.

## Getting started
See the [Getting Started](docs/getting-started.md) guide for instructions on how to install and use the toolkit.

## Features
- **MVVM Support**: Provides base classes and utilities to implement the MVVM pattern effectively.
- **GenericHost Support**: Integrates with .NET Generic Host for dependency injection and configuration.
- **Navigation Management**: Simplifies navigation for views and view models.

## Recommendations
We recommend using this alongside CommunityToolkit.Mvvm for a complete MVVM experience.

## FAQ
Q: Why use Meteion.Toolkit.WPF.MVVM and .Hosting instead of CommunityToolkit.IoC?
A: The CommunityToolkit.IoC is a simple IoC container that is not designed for complex scenarios. Meteion.Toolkit.WPF.MVVM and .Hosting provide a more robust solution for dependency injection and application hosting in WPF applications. It really helps makes your application feel like WPF was designed with Dependency Injection in mind. It also supplies some opinionated defaults, or can be adapted to your use case.