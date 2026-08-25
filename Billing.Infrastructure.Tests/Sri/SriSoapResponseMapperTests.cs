using Ecunexo.Billing.Domain.Sri;
using Ecunexo.Billing.Infrastructure.Sri;

namespace Ecunexo.Billing.Infrastructure.Tests.Sri;

public class SriSoapResponseMapperTests
{
    [Fact(DisplayName = "Recepción RECIBIDA se mapea a Received")]
    public void MapReception_Recibida_ReturnsReceived()
    {
        const string soap = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <RespuestaRecepcionComprobante>
                  <estado>RECIBIDA</estado>
                </RespuestaRecepcionComprobante>
              </soap:Body>
            </soap:Envelope>
            """;
        var key = new string('1', 49);

        var result = SriSoapResponseMapper.MapReception(soap, key);

        Assert.Equal(SriTransmissionState.Received, result.State);
        Assert.Equal(key, result.AccessKey);
    }

    [Fact(DisplayName = "Autorización AUTORIZADO exige XML y fecha")]
    public void MapAuthorization_Autorizado_ReturnsAuthorized()
    {
        const string soap = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <RespuestaAutorizacionComprobante>
                  <autorizaciones>
                    <autorizacion>
                      <estado>AUTORIZADO</estado>
                      <numeroAutorizacion>2101202401179214673900110010010000000011234567812</numeroAutorizacion>
                      <fechaAutorizacion>2024-01-21T10:00:00-05:00</fechaAutorizacion>
                      <comprobante>&lt;factura/&gt;</comprobante>
                    </autorizacion>
                  </autorizaciones>
                </RespuestaAutorizacionComprobante>
              </soap:Body>
            </soap:Envelope>
            """;
        var key = "2101202401179214673900110010010000000011234567812";

        var result = SriSoapResponseMapper.MapAuthorization(soap, key);

        Assert.Equal(SriTransmissionState.Authorized, result.State);
        Assert.False(string.IsNullOrWhiteSpace(result.AuthorizedXml));
        Assert.NotNull(result.AuthorizationDate);
    }

    [Fact(DisplayName = "Autorización EN PROCESAMIENTO se mapea a Processing")]
    public void MapAuthorization_EnProcesamiento_ReturnsProcessing()
    {
        const string soap = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <autorizacion>
                  <estado>EN PROCESAMIENTO</estado>
                </autorizacion>
              </soap:Body>
            </soap:Envelope>
            """;
        var key = new string('2', 49);

        var result = SriSoapResponseMapper.MapAuthorization(soap, key);

        Assert.Equal(SriTransmissionState.Processing, result.State);
    }

    [Fact(DisplayName = "Recepción DEVUELTA con mensajes anidados del SRI")]
    public void MapReception_Devuelta_ParsesNestedMessages()
    {
        const string soap = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <ns2:validarComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.recepcion">
                  <RespuestaRecepcionComprobante>
                    <estado>DEVUELTA</estado>
                    <comprobantes>
                      <comprobante>
                        <claveAcceso>N/A</claveAcceso>
                        <mensajes>
                          <mensaje>
                            <identificador>35</identificador>
                            <mensaje>ARCHIVO NO CUMPLE ESTRUCTURA XML</mensaje>
                            <informacionAdicional>detalle</informacionAdicional>
                            <tipo>ERROR</tipo>
                          </mensaje>
                        </mensajes>
                      </comprobante>
                    </comprobantes>
                  </RespuestaRecepcionComprobante>
                </ns2:validarComprobanteResponse>
              </soap:Body>
            </soap:Envelope>
            """;
        var key = new string('1', 49);

        var result = SriSoapResponseMapper.MapReception(soap, key);

        Assert.Equal(SriTransmissionState.Returned, result.State);
        Assert.Equal(key, result.AccessKey);
        Assert.Single(result.Messages);
        Assert.Equal("35", result.Messages[0].Identifier);
        Assert.Equal("ARCHIVO NO CUMPLE ESTRUCTURA XML", result.Messages[0].Text);
    }

    [Fact(DisplayName = "Autorización sin comprobantes se mapea a Processing")]
    public void MapAuthorization_EmptyAutorizaciones_ReturnsProcessing()
    {
        const string soap = """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <ns2:autorizacionComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.autorizacion">
                  <RespuestaAutorizacionComprobante>
                    <claveAccesoConsultada>2101202401179214673900110010010000000011234567812</claveAccesoConsultada>
                    <numeroComprobantes>0</numeroComprobantes>
                    <autorizaciones/>
                  </RespuestaAutorizacionComprobante>
                </ns2:autorizacionComprobanteResponse>
              </soap:Body>
            </soap:Envelope>
            """;
        var key = "2101202401179214673900110010010000000011234567812";

        var result = SriSoapResponseMapper.MapAuthorization(soap, key);

        Assert.Equal(SriTransmissionState.Processing, result.State);
        Assert.Contains(result.Messages, m => m.Identifier == "SRI_AUTH_EMPTY");
    }
}
