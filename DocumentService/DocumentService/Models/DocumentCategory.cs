namespace DocumentService.Models;

/// <summary>
/// Catégories de documents gérées par le centre, alignées sur le cahier
/// des charges (procédures internes, notes de service, circulaires, etc.).
/// Stocké en base sous forme de texte (voir DocumentDbContext) pour rester
/// lisible directement en SQL.
/// </summary>
public enum DocumentCategory
{
    ProcedureInterne,
    NoteDeService,
    Circulaire,
    GuideUtilisation,
    DocumentQualite,
    FormulaireAdministratif,
    RapportAudit,
    CorrespondanceOfficielle,
    Autre
}