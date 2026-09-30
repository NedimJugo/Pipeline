import React, { useState } from 'react';
import { Sliders, Award, Trophy, Sparkles, Star } from 'lucide-react';
import { OfferComparisonItem } from './types';

interface Criterion {
  id: string;
  name: string;
  weight: number; // percentage
}

const DEFAULT_CRITERIA: Criterion[] = [
  { id: 'comp', name: 'Compensation & Equity', weight: 25 },
  { id: 'growth', name: 'Career Growth & Scope', weight: 20 },
  { id: 'worklife', name: 'Work-Life & Remote Flexibility', weight: 20 },
  { id: 'culture', name: 'Team & Engineering Culture', weight: 20 },
  { id: 'benefits', name: 'Benefits & Perks', weight: 15 },
];

interface OfferScorecardWidgetProps {
  offers: OfferComparisonItem[];
}

export const OfferScorecardWidget: React.FC<OfferScorecardWidgetProps> = ({ offers }) => {
  const [criteria, setCriteria] = useState<Criterion[]>(DEFAULT_CRITERIA);
  // Scores: { [appId]: { [criterionId]: number (1-5) } }
  const [scores, setScores] = useState<Record<string, Record<string, number>>>({});

  const handleScoreChange = (appId: string, criterionId: string, rating: number) => {
    setScores((prev) => ({
      ...prev,
      [appId]: {
        ...(prev[appId] || {}),
        [criterionId]: rating,
      },
    }));
  };

  const handleWeightChange = (criterionId: string, newWeight: number) => {
    setCriteria((prev) =>
      prev.map((c) => (c.id === criterionId ? { ...c, weight: Math.max(0, Math.min(100, newWeight)) } : c))
    );
  };

  // Calculate composite score for each offer
  const totalWeight = criteria.reduce((sum, c) => sum + c.weight, 0) || 1;

  const offerResults = offers.map((offer) => {
    const appScores = scores[offer.applicationId] || {};
    let weightedPoints = 0;

    criteria.forEach((criterion) => {
      // Default to 3/5 if unrated
      const rating = appScores[criterion.id] ?? 3;
      weightedPoints += criterion.weight * (rating / 5);
    });

    const finalScore = Math.round((weightedPoints / totalWeight) * 100);

    return {
      offer,
      finalScore,
    };
  });

  // Sort by final score descending
  const rankedOffers = [...offerResults].sort((a, b) => b.finalScore - a.finalScore);

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-sm space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-border">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-primary/10 text-primary">
            <Sliders className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight">Weighted Decision Matrix</h2>
            <p className="text-xs text-muted-foreground">
              Customize weights for what matters most to you and score each offer from 1 to 5
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {rankedOffers.length > 0 && (
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-500 text-xs font-bold">
              <Trophy className="h-4 w-4" />
              <span>Top Pick: {rankedOffers[0].offer.companyName} ({rankedOffers[0].finalScore}/100)</span>
            </div>
          )}
        </div>
      </div>

      {/* Criteria Weight Sliders */}
      <div className="space-y-3 bg-muted/20 p-4 rounded-xl border border-border/50">
        <div className="flex items-center justify-between text-xs font-semibold text-muted-foreground uppercase tracking-wider">
          <span>Decision Criteria</span>
          <span>Importance Weight</span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {criteria.map((c) => (
            <div key={c.id} className="space-y-1.5 bg-background p-3 rounded-lg border border-border">
              <div className="flex justify-between items-center text-xs">
                <span className="font-semibold text-foreground">{c.name}</span>
                <span className="font-bold text-primary">{c.weight}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="50"
                step="5"
                value={c.weight}
                onChange={(e) => handleWeightChange(c.id, Number(e.target.value))}
                className="w-full accent-primary h-1.5 bg-muted rounded-lg cursor-pointer"
              />
            </div>
          ))}
        </div>
      </div>

      {/* Scoring Grid */}
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="border-b border-border">
              <th className="py-3 px-4 font-semibold text-muted-foreground uppercase tracking-wider w-48">Criterion</th>
              {offers.map((offer) => {
                const rankIdx = rankedOffers.findIndex((r) => r.offer.applicationId === offer.applicationId);
                const rank = rankIdx + 1;
                const isWinner = rank === 1;

                return (
                  <th key={offer.applicationId} className="py-3 px-4 min-w-[200px]">
                    <div className="flex items-center justify-between">
                      <div>
                        <div className="font-bold text-sm text-foreground">{offer.roleTitle}</div>
                        <div className="text-muted-foreground font-medium">{offer.companyName}</div>
                      </div>
                      <span
                        className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-bold ${
                          isWinner
                            ? 'bg-amber-500/20 text-amber-500 border border-amber-500/30'
                            : 'bg-muted text-muted-foreground'
                        }`}
                      >
                        {isWinner && <Award className="h-3 w-3" />}
                        #{rank}
                      </span>
                    </div>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {criteria.map((criterion) => (
              <tr key={criterion.id} className="hover:bg-muted/10 transition-colors">
                <td className="py-3 px-4 font-medium text-foreground">
                  <div>{criterion.name}</div>
                  <div className="text-[11px] text-muted-foreground">{criterion.weight}% weight</div>
                </td>
                {offers.map((offer) => {
                  const currentRating = scores[offer.applicationId]?.[criterion.id] ?? 3;

                  return (
                    <td key={offer.applicationId} className="py-3 px-4">
                      <div className="flex items-center gap-1.5">
                        {[1, 2, 3, 4, 5].map((star) => (
                          <button
                            key={star}
                            type="button"
                            onClick={() => handleScoreChange(offer.applicationId, criterion.id, star)}
                            className={`p-1 rounded-md transition-colors ${
                              star <= currentRating
                                ? 'text-amber-400 hover:text-amber-500'
                                : 'text-muted-foreground/30 hover:text-muted-foreground/60'
                            }`}
                            title={`Rate ${star} / 5`}
                          >
                            <Star className={`h-4 w-4 ${star <= currentRating ? 'fill-current' : ''}`} />
                          </button>
                        ))}
                        <span className="text-xs font-bold text-muted-foreground ml-1.5">{currentRating}/5</span>
                      </div>
                    </td>
                  );
                })}
              </tr>
            ))}

            {/* Total composite score row */}
            <tr className="bg-primary/5 font-bold">
              <td className="py-4 px-4 text-primary text-sm flex items-center gap-1.5">
                <Sparkles className="h-4 w-4" />
                <span>Weighted Composite Score</span>
              </td>
              {offers.map((offer) => {
                const res = offerResults.find((r) => r.offer.applicationId === offer.applicationId);
                const score = res?.finalScore ?? 0;
                const isWinner = rankedOffers[0]?.offer.applicationId === offer.applicationId;

                return (
                  <td key={offer.applicationId} className="py-4 px-4">
                    <div className="flex items-center gap-2">
                      <div className="text-xl font-extrabold text-foreground">{score}</div>
                      <span className="text-xs text-muted-foreground">/ 100</span>
                      {isWinner && (
                        <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500 text-white shadow-xs">
                          Recommended
                        </span>
                      )}
                    </div>
                  </td>
                );
              })}
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
};
