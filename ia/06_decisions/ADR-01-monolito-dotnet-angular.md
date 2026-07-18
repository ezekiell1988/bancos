# ADR-01 — Monolito .NET + Angular + MSSQL

> Estado: Aceptada
> Fecha: 2026-07-18

## Contexto

Aplicación familiar local con importación de archivos, jobs y dashboard; futura publicación en contenedor Azure.

## Decisión

Usar .NET 10/C# 14 Minimal APIs por features, Angular standalone y MSSQL. Angular se compila y API lo sirve; un solo despliegue futuro.

## Consecuencias

Procesamiento y secretos quedan backend. Se evita segundo backend y runtime Node en producción. Autenticación queda fuera hasta preparación Azure.
