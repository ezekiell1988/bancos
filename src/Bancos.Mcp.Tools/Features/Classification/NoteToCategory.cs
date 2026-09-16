namespace Bancos.Mcp.Features.Classification;

public static class NoteToCategory
{
    private static readonly (string[] Keywords, string Code)[] Rules =
    [
        (["traslado", "transferencia interna", "fondos"], "asset.cash"),
        (["pago tarjeta", "tarjeta de credito", "tarjeta de crédito", "tc"], "liability.creditCard"),
        (["prestamo", "préstamo", "cuota credito", "cuota crédito", "financiamiento"], "liability.loan"),
        (["salario", "sueldo", "planilla", "pago de salario"], "income.salary"),
        (["supermercado", "viveres", "víveres", "comida", "alimentacion", "alimentación", "mercado", "sodas", "restaurante", "pasta", "cocina", "pizza", "tortilla", "pollo", "soda", "lunch", "almuerzo", "cena", "desayuno"], "expense.groceries"),
        (["gasolina", "combustible", "uber", "taxi", "bus", "transporte", "peaje", "parqueo", "reparacion vehiculo", "reparación vehiculo", "taller", "mecanico", "mecánico"], "expense.transport"),
        (["alquiler", "vivienda", "hipoteca", "casa"], "expense.housing"),
        (["luz", "electricidad", "agua", "telefono", "teléfono", "internet", "cable", "servicio publico", "servicio público"], "expense.utilities"),
        (["doctor", "medico", "médico", "farmacia", "salud", "hospital", "clinica", "clínica", "cita medica", "cita médica", "cedula", "cédula"], "expense.health"),
        (["gimnasio", "gym", "cine", "entretenimiento", "streaming", "netflix", "spotify", "deporte"], "expense.entertainment"),
        (["ingreso otro", "otros ingresos", "devolucion", "devolución", "reintegro", "cobro", "reembolso", "reversion", "reversión"], "income.other"),
        (["ropa", "zapatos", "calzado", "vestimenta", "ropa y calzado", "cuota ropa", "cuota zapatos", "ferreteria", "ferretería", "herramienta", "alicate", "apagador", "materiales", "compra hogar"], "expense.other"),
    ];

    public static string? Resolve(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        var lower = note.ToLowerInvariant();
        foreach (var (keywords, code) in Rules)
        {
            if (keywords.Any(k => lower.Contains(k)))
                return code;
        }

        return null;
    }
}
