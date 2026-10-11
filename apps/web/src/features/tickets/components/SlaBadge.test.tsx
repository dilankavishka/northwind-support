import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { SLA_CONFIG } from '../sla';
import { SlaBadge } from './SlaBadge';

describe('SlaBadge', () => {
  const statuses: SlaStatus[] = ['WithinSla', 'AtRisk', 'Breached', 'Met', 'NotApplicable'];

  it.each(statuses)('renders %s with distinct text and non-color symbol', (status) => {
    render(<SlaBadge status={status} />);

    const config = SLA_CONFIG[status];
    expect(screen.getByText(config.text)).toBeInTheDocument();
    expect(screen.getByText(config.symbol)).toBeInTheDocument();
  });

  it('renders all 5 states distinguishably from one another', () => {
    const { container } = render(
      <div>
        {statuses.map((s) => (
          <SlaBadge key={s} status={s} />
        ))}
      </div>,
    );

    const renderedTexts = statuses.map((s) => SLA_CONFIG[s].text);
    const renderedSymbols = statuses.map((s) => SLA_CONFIG[s].symbol);

    // Ensure all 5 labels are unique
    expect(new Set(renderedTexts).size).toBe(5);
    // Ensure all 5 symbols are unique
    expect(new Set(renderedSymbols).size).toBe(5);

    // Verify all badges exist in container
    statuses.forEach((s) => {
      expect(container.querySelector(`[data-testid="sla-badge-${s}"]`)).toBeInTheDocument();
    });
  });
});
