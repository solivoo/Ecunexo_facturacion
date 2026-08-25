
namespace Ecunexo.Billing.Domain;


/*
    Ruc: es el identificador único de operaciones tributarias de una empresa
    sirve como codigo permiso para emitir comprobantes fisicos o electronicos.

        - El ruc es un numero público es de acceso global.        
        - El ruc de ninguna manera puede ser modificado en el sistema.
        - No se debe sobreescribir en ninguna parte del flujo de logica.
        - El Ruc es de tipo strig ya que no se hace ningun tipo de calculo sobre el.

    Patron: Factory Method
        Método estático que construye el objeto. Frente a un constructor público, permite
        rechazar datos inválidos antes de materializar la instancia.

    Alcance:
        Posición: Domain
        Clase:
            Acceso: Es de acceso Publico
            Modificador: (sealed) nadie la puede heredar            
            Tipo: (record) Igualadad por valor | Inmutable      
            
        Metodos:
            Nombre: Create
                Acceso: (Public) consultar el Ruc.

            Nombre: ToString
                Acceso: (Public) consulta el Ruc retornando del valor tipo string           
*/


public sealed record Ruc{
    public string Value { get; } = string.Empty;
    private Ruc(string value) => Value = value;
    
    public static Ruc Create(string value)
    {
        if(string.IsNullOrEmpty(value))
            throw new ArgumentException("El RUC no puede ser nulo o vacío");

        if(value.Length != 13)
            throw new ArgumentException("El RUC debe tener 13 dígitos");

        if(!value.All(char.IsDigit))
            throw new ArgumentException("El RUC debe contener solo dígitos");

        return new Ruc(value);
    }

    public override string ToString() => Value;
}
