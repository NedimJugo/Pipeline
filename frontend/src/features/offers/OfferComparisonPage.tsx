import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  Scale,
  DollarSign,
  Calendar,
  Clock,
  Briefcase,
  MapPin,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Plus,
  ArrowRight,
  ExternalLink,
} from 'lucide-react';
import { useAvailableOffers, useOfferComparison } from './useOffers';
import { OfferScorecardWidget } from './OfferScorecardWidget';

export const OfferComparisonPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();

  // Get selected application IDs from URL query
  const queryIds = searchParams.getAll('ids');
  const [selectedIds, setSelectedIds] = useState<string[]>(queryIds);

  const { data: rawAvailableOffers, isLoading: isLoadingAvailable } = useAvailableOffers();
  const availableOffers = Array.isArray(rawAvailableOffers) ? rawAvailableOffers : [];
  const { data: comparisonData, isLoading: isLoadingComparison } = useOfferComparison(
    selectedIds.length > 0 ? selectedIds : undefined
  );

  // If query params change, update state
  useEffect(() => {
    const ids = searchParams.getAll('ids');
    if (ids.length > 0) {
      setSelectedIds(ids);
    } else if (availableOffers.length > 0 && selectedIds.length === 0) {
      // Default select up to 3 available offers
      const defaultIds = availableOffers.slice(0, 3).map((o) => o.applicationId);
      setSelectedIds(defaultIds);
      const params = new URLSearchParams();
      defaultIds.forEach((id) => params.append('ids', id));
      setSearchParams(params);
    }
  }, [searchParams, availableOffers]);

  const toggleSelectOffer = (appId: string) => {
    let next: string[];
    if (selectedIds.includes(appId)) {
      next = selectedIds.filter((id) => id !== appId);
    } else {
      if (selectedIds.length >= 4) {
        alert('You can compare a maximum of 4 offers simultaneously.');
        return;
      }
      next = [...selectedIds, appId];
    }
    setSelectedIds(next);
    const params = new URLSearchParams();
    next.forEach((id) => params.append('ids', id));
    setSearchParams(params);
  };

  const offers = comparisonData?.offers || [];

  const formatCurrency = (val?: number | null, curr = 'USD') => {
    if (val == null) return '—';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: curr,
      maximumFractionDigits: 0,
    }).format(val);
  };

  return (
    <div className="max-w-7xl mx-auto space-y-8">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Offer Comparison & Decision Matrix</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Evaluate compensation packages, benefits, work modes, and rank offers with your custom criteria
          </p>
        </div>

        <button
          onClick={() => navigate('/applications')}
          className="inline-flex items-center gap-2 px-3 py-2 bg-secondary text-secondary-foreground text-xs font-semibold rounded-lg hover:bg-secondary/80 transition-colors shadow-2xs self-start sm:self-auto"
        >
          <Briefcase className="h-4 w-4" />
          <span>View All Applications</span>
        </button>
      </div>

      {/* Offer Selection Tray */}
      <div className="bg-card border border-border rounded-xl p-5 shadow-xs space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Select 2 to 4 offers to compare ({selectedIds.length} / 4 selected)
          </span>
        </div>

        {availableOffers.length === 0 ? (
          <div className="p-4 rounded-lg bg-muted/40 text-center text-xs text-muted-foreground">
            No active offers found. Move applications to the "Offer" status or fill in offer salary details to compare them.
          </div>
        ) : (
          <div className="flex flex-wrap gap-2">
            {availableOffers.map((app) => {
              const isSelected = selectedIds.includes(app.applicationId);
              return (
                <button
                  key={app.applicationId}
                  onClick={() => toggleSelectOffer(app.applicationId)}
                  className={`px-3 py-2 rounded-lg text-xs font-semibold border transition-all flex items-center gap-2 ${
                    isSelected
                      ? 'bg-primary text-primary-foreground border-primary shadow-xs'
                      : 'bg-background text-foreground border-border hover:border-primary/40'
                  }`}
                >
                  <span className="font-bold">{app.roleTitle}</span>
                  <span className="opacity-80">@ {app.companyName}</span>
                  {app.totalCompensation > 0 && (
                    <span className="opacity-90 font-mono text-[11px]">
                      ({formatCurrency(app.totalCompensation, app.currency)})
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Comparison Grid */}
      {offers.length < 2 ? (
        <div className="text-center py-16 border border-dashed border-border rounded-xl bg-card/20 space-y-3">
          <div className="h-12 w-12 rounded-full bg-primary/10 text-primary flex items-center justify-center mx-auto">
            <Scale className="h-6 w-6" />
          </div>
          <h3 className="font-bold text-sm text-foreground">Select at least 2 offers</h3>
          <p className="text-xs text-muted-foreground max-w-sm mx-auto">
            Choose multiple job offers above to compare side-by-side compensation, pros/cons, and weighted scores.
          </p>
        </div>
      ) : (
        <div className="space-y-8">
          <div className="bg-card border border-border rounded-xl overflow-hidden shadow-sm">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs border-collapse">
                <thead>
                  <tr className="border-b border-border bg-muted/30">
                    <th className="py-4 px-5 font-semibold text-muted-foreground uppercase tracking-wider w-56">
                      Offer Dimension
                    </th>
                    {offers.map((offer) => (
                      <th key={offer.applicationId} className="py-4 px-5 min-w-[240px] align-top">
                        <div className="space-y-1">
                          <h3 className="font-bold text-base text-foreground tracking-tight">{offer.roleTitle}</h3>
                          <div className="text-sm font-semibold text-primary">{offer.companyName}</div>
                          <div className="pt-2 flex items-center gap-1.5">
                            <button
                              onClick={() => navigate(`/applications/${offer.applicationId}`)}
                              className="text-[11px] text-muted-foreground hover:text-foreground flex items-center gap-1 underline underline-offset-2"
                            >
                              <span>Open Application</span>
                              <ExternalLink className="h-3 w-3" />
                            </button>
                          </div>
                        </div>
                      </th>
                    ))}
                  </tr>
                </thead>

                <tbody className="divide-y divide-border">
                  {/* Total Compensation */}
                  <tr className="bg-primary/5 font-semibold">
                    <td className="py-3.5 px-5 text-primary flex items-center gap-2">
                      <DollarSign className="h-4 w-4" />
                      <span>Total Compensation (Yr 1)</span>
                    </td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3.5 px-5">
                        <div className="text-base font-extrabold text-foreground">
                          {formatCurrency(offer.totalCompensation, offer.currency)}
                        </div>
                        <div className="text-[11px] text-muted-foreground">Base + Bonuses</div>
                      </td>
                    ))}
                  </tr>

                  {/* Base Salary */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Base Salary</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 font-mono text-sm text-foreground">
                        {formatCurrency(offer.offerSalary, offer.currency)}
                      </td>
                    ))}
                  </tr>

                  {/* Bonus */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Bonus (Sign-on / Annual)</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 font-mono text-sm text-foreground">
                        {formatCurrency(offer.offerBonus, offer.currency)}
                      </td>
                    ))}
                  </tr>

                  {/* Work Mode & Location */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Work Mode & Location</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 space-y-1">
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold bg-muted text-muted-foreground">
                          {offer.workMode}
                        </span>
                        {offer.location && (
                          <div className="text-muted-foreground flex items-center gap-1">
                            <MapPin className="h-3 w-3" />
                            <span>{offer.location}</span>
                          </div>
                        )}
                      </td>
                    ))}
                  </tr>

                  {/* Response Deadline */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Response Deadline</td>
                    {offers.map((offer) => {
                      const isExpired = offer.daysUntilDeadline != null && offer.daysUntilDeadline < 0;
                      const isUrgent = offer.daysUntilDeadline != null && offer.daysUntilDeadline <= 3 && !isExpired;

                      return (
                        <td key={offer.applicationId} className="py-3 px-5">
                          {offer.offerDeadline ? (
                            <div className="space-y-1">
                              <div className="font-medium text-foreground">
                                {new Date(offer.offerDeadline).toLocaleDateString(undefined, {
                                  month: 'short',
                                  day: 'numeric',
                                  year: 'numeric',
                                })}
                              </div>
                              {offer.daysUntilDeadline != null && (
                                <span
                                  className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold ${
                                    isExpired
                                      ? 'bg-destructive/10 text-destructive'
                                      : isUrgent
                                      ? 'bg-amber-500/10 text-amber-500'
                                      : 'bg-emerald-500/10 text-emerald-500'
                                  }`}
                                >
                                  <Clock className="h-3 w-3" />
                                  <span>
                                    {isExpired
                                      ? 'Expired'
                                      : `${offer.daysUntilDeadline} ${
                                          offer.daysUntilDeadline === 1 ? 'day' : 'days'
                                        } remaining`}
                                  </span>
                                </span>
                              )}
                            </div>
                          ) : (
                            <span className="text-muted-foreground">No deadline specified</span>
                          )}
                        </td>
                      );
                    })}
                  </tr>

                  {/* Benefits & Perks */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Benefits & Perks</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 text-muted-foreground whitespace-pre-line leading-relaxed">
                        {offer.offerBenefits || '—'}
                      </td>
                    ))}
                  </tr>

                  {/* Pros & Cons */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Pros & Highlights</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 text-emerald-400/90 whitespace-pre-line leading-relaxed">
                        {offer.pros || '—'}
                      </td>
                    ))}
                  </tr>
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Cons & Risks</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 text-destructive/80 whitespace-pre-line leading-relaxed">
                        {offer.cons || '—'}
                      </td>
                    ))}
                  </tr>

                  {/* Negotiation Strategy */}
                  <tr>
                    <td className="py-3 px-5 font-medium text-foreground">Negotiation Notes</td>
                    {offers.map((offer) => (
                      <td key={offer.applicationId} className="py-3 px-5 text-muted-foreground italic leading-relaxed">
                        {offer.offerNegotiationNotes ? `"${offer.offerNegotiationNotes}"` : '—'}
                      </td>
                    ))}
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Interactive Decision Matrix */}
          <OfferScorecardWidget offers={offers} />
        </div>
      )}
    </div>
  );
};
