import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { TicketEscalation } from '../../../types/api';
import { EscalationHistory } from './EscalationHistory';

describe('EscalationHistory', () => {
  it('renders empty message when no escalations exist', () => {
    render(<EscalationHistory escalations={[]} />);
    expect(screen.getByText(/no escalations recorded for this ticket/i)).toBeInTheDocument();
  });

  it('renders loading indicator when isLoading is true', () => {
    render(<EscalationHistory escalations={[]} isLoading />);
    expect(screen.getByText(/loading escalations/i)).toBeInTheDocument();
  });

  it('renders history entries with priority, agent, due dates, actor, and reason', () => {
    const escalations: TicketEscalation[] = [
      {
        id: 1,
        ticketId: 10,
        fromPriority: 'Medium',
        toPriority: 'High',
        fromAgent: { id: 1, fullName: 'Alice Johnson' },
        toAgent: { id: 2, fullName: 'Bob Smith' },
        fromDueAtUtc: '2026-10-11T16:00:00Z',
        toDueAtUtc: '2026-10-11T12:00:00Z',
        reason: 'Customer reported major revenue disruption',
        escalatedBy: 'TeamLead',
        escalatedAtUtc: '2026-10-11T10:00:00Z',
      },
    ];

    render(<EscalationHistory escalations={escalations} />);

    expect(screen.getByText('TeamLead')).toBeInTheDocument();
    expect(screen.getByText(/Customer reported major revenue disruption/)).toBeInTheDocument();
    expect(screen.getByText('Alice Johnson → Bob Smith', { exact: false })).toBeInTheDocument();
    expect(screen.getByText('Medium')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
  });
});
