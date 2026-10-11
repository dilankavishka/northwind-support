import type { BadgeTone } from '../../components/Badge';
import type { SlaStatus } from '../../types/api';

export interface SlaConfig {
  text: string;
  tone: BadgeTone;
  symbol: string;
}

export const SLA_CONFIG: Record<SlaStatus, SlaConfig> = {
  WithinSla: { text: 'Within SLA', tone: 'info', symbol: '●' },
  AtRisk: { text: 'At risk', tone: 'warning', symbol: '▲' },
  Breached: { text: 'Breached', tone: 'danger', symbol: '✕' },
  Met: { text: 'Met', tone: 'success', symbol: '✓' },
  NotApplicable: { text: 'No SLA', tone: 'neutral', symbol: '—' },
};

export const SLA_STATUSES: SlaStatus[] = [
  'WithinSla',
  'AtRisk',
  'Breached',
  'Met',
  'NotApplicable',
];
