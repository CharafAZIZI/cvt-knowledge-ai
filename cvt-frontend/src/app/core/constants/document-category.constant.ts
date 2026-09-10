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

// Classes Tailwind pour un badge coloré par catégorie (repérage visuel rapide).
export function getCategoryBadgeClasses(value: DocumentCategory): string {
  switch (value) {
    case 'ProcedureInterne':
      return 'bg-blue-50 text-blue-700 ring-blue-600/20';
    case 'NoteDeService':
      return 'bg-purple-50 text-purple-700 ring-purple-600/20';
    case 'Circulaire':
      return 'bg-amber-50 text-amber-700 ring-amber-600/20';
    case 'GuideUtilisation':
      return 'bg-teal-50 text-teal-700 ring-teal-600/20';
    case 'DocumentQualite':
      return 'bg-emerald-50 text-emerald-700 ring-emerald-600/20';
    case 'FormulaireAdministratif':
      return 'bg-indigo-50 text-indigo-700 ring-indigo-600/20';
    case 'RapportAudit':
      return 'bg-rose-50 text-rose-700 ring-rose-600/20';
    case 'CorrespondanceOfficielle':
      return 'bg-cyan-50 text-cyan-700 ring-cyan-600/20';
    default:
      return 'bg-ink-100 text-ink-600 ring-ink-500/10';
  }
}