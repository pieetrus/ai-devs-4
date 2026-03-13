using AiDevs.Shared;

DotEnv.Load();

var declaration = """
    SYSTEM PRZESYŁEK KONDUKTORSKICH - DEKLARACJA ZAWARTOŚCI
    ======================================================
    DATA: 2026-03-12
    PUNKT NADAWCZY: Gdańsk
    ------------------------------------------------------
    NADAWCA: 450202122
    PUNKT DOCELOWY: Żarnowiec
    TRASA: X-01
    ------------------------------------------------------
    KATEGORIA PRZESYŁKI: A
    ------------------------------------------------------
    OPIS ZAWARTOŚCI (max 200 znaków): kasety z paliwem do reaktora
    ------------------------------------------------------
    DEKLAROWANA MASA (kg): 2800
    ------------------------------------------------------
    WDP: 4
    ------------------------------------------------------
    UWAGI SPECJALNE:
    ------------------------------------------------------
    KWOTA DO ZAPŁATY: 0 PP
    ------------------------------------------------------
    OŚWIADCZAM, ŻE PODANE INFORMACJE SĄ PRAWDZIWE.
    BIORĘ NA SIEBIE KONSEKWENCJĘ ZA FAŁSZYWE OŚWIADCZENIE.
    ======================================================
    """;

Console.WriteLine("Submitting SPK declaration:");
Console.WriteLine(declaration);

var client = new AiDevsClient();
var result = await client.VerifyAsync("sendit", new { declaration });

Console.WriteLine($"\nHub response: {result}");
