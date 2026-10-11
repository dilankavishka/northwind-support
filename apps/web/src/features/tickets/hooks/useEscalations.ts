import { useCallback } from 'react';
import { ticketsApi } from '../../../api/tickets';
import { useAsyncData } from '../../../hooks/useAsyncData';
import type { TicketEscalation } from '../../../types/api';

/** Loads escalation history for a ticket, refetching whenever the ticket ID changes. */
export function useEscalations(ticketId: number) {
  const load = useCallback(
    () => (Number.isNaN(ticketId) ? Promise.resolve([]) : ticketsApi.getEscalations(ticketId)),
    [ticketId],
  );

  return useAsyncData<TicketEscalation[]>(load, 'Unable to load escalation history.');
}
