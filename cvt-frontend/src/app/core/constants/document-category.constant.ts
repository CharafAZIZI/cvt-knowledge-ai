export type DocumentCategory =
  | 'ProcedureInterne'
  | 'NoteDeService'
  | 'Circulaire'
  | 'GuideUtilisation'
  | 'DocumentQualite'
  | 'FormulaireAdministratif'
  | 'RapportAudit'
  | 'CorrespondanceOfficielle'
  | 'Autre';

export interface DocumentCategoryOption {
  value: DocumentCategory;
  label: string;
}

// Labels lisibles affichés dans l'UI (menus déroulants, badges...).
export const DOCUMENT_CATEGORIES: DocumentCategoryOption[] = [
  { value: 'ProcedureInterne', label: 'Procédure interne' },
  { value: 'NoteDeService', label: 'Note de service' },
  { value: 'Circulaire', label: 'Circulaire' },
  { value: 'GuideUtilisation', label: "Guide d'utilisation" },
  { value: 'DocumentQualite', label: 'Document qualité' },
  { value: 'FormulaireAdministratif', label: 'Formulaire administratif' },
  { value: 'RapportAudit', label: "Rapport d'audit" },
  { value: 'CorrespondanceOfficielle', label: 'Correspondance officielle' },
  { value: 'Autre', label: 'Autre' },
];

export function getCategoryLabel(value: DocumentCategory): string {
  return DOCUMENT_CATEGORIES.find((c) => c.value === value)?.label ?? value;
}