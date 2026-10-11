import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { ticketDetail } from '../../../test/fixtures';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    escalateTicket: vi.fn(),
  },
}));

const mockEscalate = vi.mocked(ticketsApi.escalateTicket);

describe('EscalationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('blocks submission and does not call API when reason is too short (< 5 characters)', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();
    const ticket = ticketDetail({ priority: 'High', status: 'Open' });

    render(<EscalationPanel ticket={ticket} onEscalated={onEscalated} />);

    const reasonInput = screen.getByLabelText(/reason for escalation/i);
    const submitButton = screen.getByRole('button', { name: /escalate ticket/i });

    // Type 4 characters
    await user.type(reasonInput, 'Urgt');
    await user.click(submitButton);

    expect(screen.getByText(/reason must be between 5 and 500 characters/i)).toBeInTheDocument();
    expect(mockEscalate).not.toHaveBeenCalled();
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('displays a 409 conflict error from the server and leaves ticket unchanged', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();
    const ticket = ticketDetail({ id: 42, priority: 'High', status: 'Open' });

    // Mock 409 Conflict rejection
    mockEscalate.mockRejectedValueOnce(
      new ApiError(409, 'Ticket cannot be escalated because it has already reached Critical priority.'),
    );

    render(<EscalationPanel ticket={ticket} onEscalated={onEscalated} />);

    const reasonInput = screen.getByLabelText(/reason for escalation/i);
    const submitButton = screen.getByRole('button', { name: /escalate ticket/i });

    await user.type(reasonInput, 'Customer escalation via account executive');
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockEscalate).toHaveBeenCalledTimes(1);
    });

    // 409 error message is visible to user
    const errorAlert = await screen.findByTestId('escalation-error');
    expect(errorAlert).toHaveTextContent(
      'Ticket cannot be escalated because it has already reached Critical priority.',
    );

    // onEscalated callback was not triggered (ticket remains in its existing state)
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('disables escalation control when ticket is already at Critical priority', () => {
    const onEscalated = vi.fn();
    const ticket = ticketDetail({ priority: 'Critical', status: 'Open' });

    render(<EscalationPanel ticket={ticket} onEscalated={onEscalated} />);

    const submitButton = screen.getByRole('button', { name: /escalate ticket/i });
    expect(submitButton).toBeDisabled();
    expect(screen.getByText(/already at Critical priority/i)).toBeInTheDocument();
  });

  it('disables escalation control when ticket is Resolved or Closed', () => {
    const onEscalated = vi.fn();
    const resolvedTicket = ticketDetail({ priority: 'Medium', status: 'Resolved' });

    const { rerender } = render(<EscalationPanel ticket={resolvedTicket} onEscalated={onEscalated} />);
    expect(screen.getByRole('button', { name: /escalate ticket/i })).toBeDisabled();
    expect(screen.getByText(/resolved or closed tickets cannot be escalated/i)).toBeInTheDocument();

    const closedTicket = ticketDetail({ priority: 'Medium', status: 'Closed' });
    rerender(<EscalationPanel ticket={closedTicket} onEscalated={onEscalated} />);
    expect(screen.getByRole('button', { name: /escalate ticket/i })).toBeDisabled();
  });

  it('submits valid reason, calls API, and invokes onEscalated callback', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();
    const ticket = ticketDetail({ id: 15, priority: 'Medium', status: 'Open' });

    const response = {
      ticket: { ...ticket, priority: 'High' as const },
      escalation: {
        id: 1,
        ticketId: 15,
        fromPriority: 'Medium' as const,
        toPriority: 'High' as const,
        fromAgent: null,
        toAgent: null,
        fromDueAtUtc: null,
        toDueAtUtc: null,
        reason: 'Customer SLA SLA is close to breach',
        escalatedBy: 'Dispatcher',
        escalatedAtUtc: '2026-10-11T12:00:00Z',
      },
    };

    mockEscalate.mockResolvedValueOnce(response);

    render(<EscalationPanel ticket={ticket} onEscalated={onEscalated} />);

    const reasonInput = screen.getByLabelText(/reason for escalation/i);
    await user.type(reasonInput, 'Customer SLA SLA is close to breach');

    const submitButton = screen.getByRole('button', { name: /escalate ticket/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockEscalate).toHaveBeenCalledWith(15, {
        reason: 'Customer SLA SLA is close to breach',
        escalatedBy: 'Dispatcher',
      });
      expect(onEscalated).toHaveBeenCalledWith(response);
    });

    // Reason field is cleared after success
    expect(reasonInput).toHaveValue('');
  });
});
