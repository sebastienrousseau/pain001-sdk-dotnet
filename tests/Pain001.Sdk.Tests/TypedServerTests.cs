// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;

namespace Pain001.Sdk.Tests;

/// <summary>
/// One test per typed method against the server at <c>PAIN001_BASE_URL</c>,
/// with the kit's fixtures. Skipped when that variable is unset.
/// </summary>
public sealed class TypedServerTests
{
    private const string CreditTransferCsv = "fixtures/credit-transfer.pain.001.001.09.csv";
    private const string CreditTransferXml = "fixtures/credit-transfer.pain.001.001.09.xml";

    [ServerFact]
    public async Task HealthReportsHealthy()
    {
        using Pain001Client client = LiveServer.CreateClient();

        HealthResponse health = await client.HealthAsync();

        Assert.Equal("healthy", health.Status);
        Assert.False(string.IsNullOrEmpty(health.Version));
    }

    [ServerFact]
    public async Task ValidateCountsTheRows()
    {
        using Pain001Client client = LiveServer.CreateClient();

        ValidationResponse result = await client.ValidateAsync(
            Kit.ReadText(CreditTransferCsv), "payments.csv", MessageTypes.Pain001V09);

        Assert.True(result.IsValid);
        Assert.Equal(5, result.TotalRows);
        Assert.Equal(5, result.ValidRows);
        Assert.Equal(0, result.InvalidRows);
    }

    [ServerFact]
    public async Task ValidateRejectsABadIbanWithA400()
    {
        using Pain001Client client = LiveServer.CreateClient();

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.ValidateAsync(
            Kit.ReadText("fixtures/bad-iban.pain.001.001.09.csv"), "payments.csv", MessageTypes.Pain001V09));

        Assert.Equal(400, error.StatusCode);
        Assert.Contains("not a valid IBAN", error.Detail.GetString(), StringComparison.Ordinal);
    }

    [ServerFact]
    public async Task GenerateReturnsTheXml()
    {
        using Pain001Client client = LiveServer.CreateClient();

        UiGenerateResponse result = await client.GenerateAsync(
            Kit.ReadText(CreditTransferCsv), "payments.csv", MessageTypes.Pain001V09);

        Assert.True(result.Success);
        Assert.Contains("<NbOfTxs>5</NbOfTxs>", result.Xml, StringComparison.Ordinal);
        Assert.Contains("<CtrlSum>1100.00</CtrlSum>", result.Xml, StringComparison.Ordinal);
    }

    [ServerFact]
    public async Task GenerateRejectsAnUnknownMessageTypeWithA422()
    {
        using Pain001Client client = LiveServer.CreateClient();

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.GenerateAsync(
            Kit.ReadText(CreditTransferCsv), "payments.csv", "pain.999.001.01"));

        Assert.Equal(422, error.StatusCode);
        Assert.Equal(JsonValueKind.Array, error.Detail.ValueKind);
    }

    [ServerFact]
    public async Task CheckPassesTheFixtureDocument()
    {
        using Pain001Client client = LiveServer.CreateClient();

        CheckResponse result = await client.CheckAsync(Kit.ReadText(CreditTransferXml));

        Assert.True(result.Passed);
        Assert.Equal(0, result.Errors);
        Assert.Equal(MessageTypes.Pain001V09, result.MessageType);
        Assert.NotEmpty(result.Rungs);
    }

    [ServerFact]
    public async Task FacesListsTheThreeFaces()
    {
        using Pain001Client client = LiveServer.CreateClient();

        IReadOnlyList<FaceInfo> faces = await client.FacesAsync();

        Assert.Contains(faces, face => face.Name == "berlin_group");
        Assert.Contains(faces, face => face.Name == "iso_2018");
        Assert.Contains(faces, face => face.Name == "uk_open_banking");
    }

    [ServerFact]
    public async Task ProjectFaceCarriesTheDebtorIban()
    {
        using Pain001Client client = LiveServer.CreateClient();

        FaceResponse result = await client.ProjectFaceAsync("berlin_group", Kit.ReadText(CreditTransferXml));

        Assert.Equal("berlin_group", result.Face);
        Assert.Equal(MessageTypes.Pain001V09, result.Version);
        Assert.Equal(
            "DE07512108001245126162",
            result.Payload.GetProperty("debtorAccount").GetProperty("iban").GetString());
        Assert.Equal("berlin_group", result.LossReport.Face);
    }

    [ServerFact]
    public async Task RequestReturnsTheRawStatus()
    {
        using Pain001Client client = LiveServer.CreateClient();

        Pain001RawResponse response = await client.RequestAsync("GET", "/api/v1/health");

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("healthy", response.Body.GetProperty("status").GetString());
    }
}
