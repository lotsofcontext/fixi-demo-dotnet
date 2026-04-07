# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed

- **WI-101**: `CalculadoraConsumo.Calcular` no longer throws `DivideByZeroException` when two readings occur on the same day. Intra-day readings now return the raw kWh delta instead of attempting a daily average with zero days elapsed.
