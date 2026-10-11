import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ErrorMessage } from '../../components/ErrorMessage';
import { Spinner } from '../../components/Spinner';
import { useReferenceData } from '../../hooks/useReferenceData';
import { formatDateTime } from '../../lib/format';
import type { TicketDetail } from '../../types/api';
import { CreateTicketForm } from './components/CreateTicketForm';
import { PriorityBadge } from './components/TicketBadges';
import { SlaBadge } from './components/SlaBadge';

export function CreateTicketPage() {
  const reference = useReferenceData();
  const navigate = useNavigate();
  const [created, setCreated] = useState<TicketDetail>();

  return (
    <section>
      <header className="page-header">
        <div>
          <p className="breadcrumb">
            <Link to="/tickets">Tickets</Link> / New
          </p>
          <h1>Raise a ticket</h1>
        </div>
      </header>

      {reference.isLoading && <Spinner label="Loading customers and categories" />}

      {reference.error && (
        <ErrorMessage message={reference.error} onRetry={() => void reference.reload()} />
      )}

      {created && (
        <div className="card notice" role="status">
          <p style={{ fontSize: '1.05rem', marginTop: 0 }}>
            Raised <strong>{created.reference}</strong> for {created.customer.name}.
          </p>

          <div
            style={{
              margin: '1rem 0',
              padding: '0.75rem 1rem',
              background: '#f8f9fa',
              borderRadius: 'var(--radius)',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.6rem',
            }}
          >
            <h3 style={{ margin: 0, fontSize: '0.95rem' }}>Automated Triage Outcome</h3>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap', fontSize: '0.9rem' }}>
              <strong>Applied Priority:</strong>
              <PriorityBadge priority={created.priority} />
              {created.triage?.priorityReason && (
                <span className="summary__note">({created.triage.priorityReason})</span>
              )}
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap', fontSize: '0.9rem' }}>
              <strong>Due Date &amp; SLA:</strong>
              <span>{formatDateTime(created.dueAtUtc)}</span>
              <SlaBadge status={created.slaStatus} />
              {created.triage?.slaWindowReason && (
                <span className="summary__note">({created.triage.slaWindowReason})</span>
              )}
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap', fontSize: '0.9rem' }}>
              <strong>Assignment:</strong>
              <span>{created.assignedAgent?.fullName ?? 'Unassigned'}</span>
              {created.triage?.assignmentReason && (
                <span className="summary__note">({created.triage.assignmentReason})</span>
              )}
            </div>
          </div>

          <div className="button-row">
            <button
              type="button"
              className="button button--primary"
              onClick={() => void navigate(`/tickets/${created.id}`)}
            >
              Open {created.reference}
            </button>
            <button type="button" className="button button--ghost" onClick={() => setCreated(undefined)}>
              Raise another
            </button>
          </div>
        </div>
      )}

      {reference.data && !created && (
        <CreateTicketForm
          customers={reference.data.customers}
          categories={reference.data.categories}
          onCreated={setCreated}
        />
      )}
    </section>
  );
}
