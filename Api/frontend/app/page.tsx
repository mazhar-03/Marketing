"use client";

import {useState} from "react";
import {Download, Loader, RefreshCw} from "lucide-react";

function fmtMoney(n: number, decimals = 2): string {
    const fixed = n.toFixed(decimals);
    const parts = fixed.split(".");
    parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, " ");
    return "zł" + parts.join(",");
}

function fmtNum(n: number): string {
    return Math.round(n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
}

function fmtPct(n: number): string {
    // Noktayı virgüle çeviriyoruz
    return n.toFixed(2).replace(".", ",") + "%";
}

// Safe date parse — avoids UTC-to-local shift
function parseLocalDate(str: string): Date {
    const [y, m, d] = str.split("-").map(Number);
    return new Date(y, m - 1, d);
}

function toDateStr(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${y}-${m}-${day}`;
}

function getDefaultWeekStart(): string {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return toDateStr(d);
}

interface MetricConfig {
    key: string;
    label: string;
    format: (v: number) => string;
    color: string;
    isCalculated?: boolean;
}

type MetricKey = typeof AVAILABLE_METRICS[number]["key"];

const AVAILABLE_METRICS: MetricConfig[] = [
    {key: "totalSpend", label: "Total Spend", format: (v) => fmtMoney(v), color: "#2196f3"},
    {key: "totalClicks", label: "Total Clicks", format: (v) => fmtNum(v), color: "#4caf50"},
    {key: "totalImpressions", label: "Total Impressions", format: (v) => fmtNum(v), color: "#ffeb3b"},
    {key: "totalViews", label: "Total Views", format: (v) => fmtNum(v), color: "#ff9800"},
    {key: "totalConversions", label: "Total Conversions", format: (v) => fmtNum(v), color: "#e91e63"},
    {key: "conversionValue", label: "Conversion Value", format: (v) => fmtMoney(v), color: "#9c27b0"},
    {key: "ctr", label: "Avg CTR", format: (v) => fmtPct(v), color: "#f44336", isCalculated: true},
    {key: "cpc", label: "Avg CPC", format: (v) => fmtMoney(v), color: "#b388ff", isCalculated: true},
    {key: "cpm", label: "Avg CPM", format: (v) => fmtMoney(v), color: "#00e5ff", isCalculated: true},
    {key: "cpv", label: "Avg CPV", format: (v) => fmtMoney(v), color: "#00ecb3", isCalculated: true},
    {key: "cpa", label: "Avg CPA", format: (v) => fmtMoney(v), color: "#ff5722", isCalculated: true},
    {
        key: "roas",
        label: "Avg ROAS",
        format: (v) => v.toFixed(2).replace(".", ",") + "x",
        color: "#8bc34a",
        isCalculated: true
    },];

// ESKİ HALİ: interface DailyRow { spend: number; clicks: number; impressions: number; }
// YENİ HALİ:
interface DailyRow {
    spend: number;
    clicks: number;
    impressions: number;

    [key: string]: number;
}


interface CampaignRow {
    spend: number;
    clicks: number;
    impressions: number;
    conversions: number;

    [key: string]: number;
}

export default function AgencyDashboard() {
    const [apiBase, setApiBase] = useState("http://localhost:5189");
    const [clientId, setClientId] = useState(1);
    const [markup, setMarkup] = useState(1.5);
    const [weekStart, setWeekStart] = useState(getDefaultWeekStart);
    const [clientNameOverride, setClientNameOverride] = useState("");
    const [loading, setLoading] = useState(false);
    const [downloading, setDownloading] = useState(false);
    const [error, setError] = useState("");
    const [hasData, setHasData] = useState(false);

    const [clientName, setClientName] = useState("");
    const [weekLabel, setWeekLabel] = useState("");
    const [summary, setSummary] = useState({
        spend: 0,
        clicks: 0,
        impressions: 0,
        conversions: 0,
        ctr: 0,
        cpc: 0,
        cpm: 0
    });
    const [allDays, setAllDays] = useState<string[]>([]);
    const [dailyMap, setDailyMap] = useState<Record<string, DailyRow>>({});
    const [campRows, setCampRows] = useState<[string, CampaignRow][]>([]);
    const [chartData, setChartData] = useState<any[]>([]);
    const [selectedMetrics, setSelectedMetrics] = useState<string[]>([
        "totalSpend", "totalClicks", "totalImpressions", "totalConversions"
    ]);

    async function loadData() {
        setLoading(true);
        setError("");
        setHasData(false);

        // Tarih kaymasını önlemek için milisaniye bazlı temiz hesaplama
// 1. Keep the actual week end for the API to be +7 days (forces backend to include day 7)
        const ws = parseLocalDate(weekStart);
        const apiWe = new Date(ws.getTime());
        apiWe.setDate(ws.getDate() + 7); // Changed from +6 to +7 for the API query limit

        const from = toDateStr(ws);
        const to = toDateStr(apiWe); // This will now be 2026-05-11

// 2. Keep your visual label showing the correct 6-day span (May 4 to May 10)
        const visualWe = new Date(ws.getTime());
        visualWe.setDate(ws.getDate() + 6);
        const fmtLabel = (d: Date) =>
            d.toLocaleDateString("en-GB", {day: "2-digit", month: "short", year: "numeric"});
        setWeekLabel(`${fmtLabel(ws)} – ${fmtLabel(visualWe)}`);

        try {
            const [kpiRes, clientRes] = await Promise.all([
                fetch(`${apiBase}/api/clients/${clientId}/kpi?from=${from}&to=${to}&platform=GoogleAds`),
                clientNameOverride ? Promise.resolve(null) : fetch(`${apiBase}/api/clients/${clientId}`)
            ]);

            if (!kpiRes.ok) throw new Error("KPI API response error");
            const kpis: any[] = await kpiRes.json();

            let name = clientNameOverride || `Client ${clientId}`;
            if (clientRes && clientRes.ok) {
                const c = await clientRes.json();
                name = c.name || name;
            }
            setClientName(name);

            if (!kpis.length) {
                setError(`No Google Ads data found for ${from} – ${to}.`);
                setLoading(false);
                return;
            }

            const rawSpend = kpis.reduce((s, x) => s + x.totalSpend, 0);
            const totalClicks = kpis.reduce((s, x) => s + x.totalClicks, 0);
            const totalImpressions = kpis.reduce((s, x) => s + x.totalImpressions, 0);
            const totalConversions = kpis.reduce((s, x) => s + (x.totalConversions || 0), 0);
            const adjSpend = rawSpend * markup;

            setSummary({
                spend: adjSpend,
                clicks: totalClicks,
                impressions: totalImpressions,
                conversions: totalConversions,
                ctr: totalImpressions > 0 ? (totalClicks / totalImpressions) * 100 : 0,
                cpc: totalClicks > 0 ? adjSpend / totalClicks : 0,
                cpm: totalImpressions > 0 ? (adjSpend / totalImpressions) * 1000 : 0,
            });

            // Tam olarak 7 günü güvenli bir şekilde üretiyoruz
            const days: string[] = [];
            for (let i = 0; i < 7; i++) {
                const d = new Date(ws.getTime());
                d.setDate(ws.getDate() + i);
                days.push(toDateStr(d));
            }
            setAllDays(days);

            const dMap: Record<string, DailyRow> = {};
            days.forEach(d => dMap[d] = {spend: 0, clicks: 0, impressions: 0});

            kpis.forEach(x => {
                const raw: string = x.date;
                const d = raw.includes("T") ? raw.split("T")[0] : raw;
                if (dMap[d]) {
                    // Grafik ve tablolar için tüm ana metrikleri dMap'e dolduruyoruz
                    dMap[d].spend += x.totalSpend * markup;
                    dMap[d].totalSpend += x.totalSpend * markup;
                    dMap[d].clicks += x.totalClicks;
                    dMap[d].totalClicks += x.totalClicks;
                    dMap[d].impressions += x.totalImpressions;
                    dMap[d].totalImpressions += x.totalImpressions;

                    // DB'ye yeni eklenen alanlar
                    dMap[d].totalViews += x.totalViews || 0;
                    dMap[d].totalConversions += x.totalConversions || 0;
                    dMap[d].conversionValue += x.conversionValue || 0;
                }
            });
            setDailyMap(dMap);

            setChartData(days.map(d => {
                const rowObj: any = {
                    label: parseLocalDate(d).toLocaleDateString("en-GB", {
                        weekday: "short",
                        day: "2-digit",
                        month: "short"
                    })
                };
                // Seçili tüm metrikleri grafiğin data objesine yediriyoruz
                selectedMetrics.forEach(mKey => {
                    rowObj[mKey] = dMap[d][mKey]
                });
                return rowObj;
            }));

            const cMap: Record<string, CampaignRow> = {};
            kpis.forEach(x => {
                const k = x.campaignName;
                if (!cMap[k]) cMap[k] = {spend: 0, clicks: 0, impressions: 0, conversions: 0};
                cMap[k].spend += x.totalSpend * markup;
                cMap[k].clicks += x.totalClicks;
                cMap[k].impressions += x.totalImpressions;
                cMap[k].conversions += x.totalConversions || 0;
            });
            setCampRows(Object.entries(cMap).sort(([, a], [, b]) => b.spend - a.spend).slice(0, 5));

            setHasData(true);
        } catch (e: any) {
            setError(`Failed to fetch: ${e.message}. Make sure the API is running and CORS is enabled.`);
        }
        setLoading(false);
    }

    async function handleDownload() {
        setDownloading(true);
        try {
            const metricsParam = selectedMetrics.join(",");
            const url = `${apiBase}/api/clients/${clientId}/report/weekly?week=${weekStart}&markup=${markup}&metrics=${metricsParam}`;
            const res = await fetch(url);
            if (!res.ok) throw new Error("Failed to generate report");
            const blob = await res.blob();
            const link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = `google-ads-report_client${clientId}_${weekStart}.pdf`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } catch (e: any) {
            alert("Error: " + e.message);
        }
        setDownloading(false);
    }

    const customTooltip = ({active, payload, label}: any) => {
        if (!active || !payload?.length) return null;
        return (
            <div style={{
                background: "#2d2d2d",
                border: "1px solid #444",
                borderRadius: 8,
                padding: "10px 14px",
                fontSize: 12
            }}>
                <p style={{margin: "0 0 6px", fontWeight: 600, color: "#fff"}}>{label}</p>
                {payload.map((p: any) => {
                    // İlgili metriğin config ayarlarını buluyoruz
                    const config = AVAILABLE_METRICS.find(m => m.key === p.dataKey);
                    // Eğer config varsa kendi formatlayıcısını kullan, yoksa ham sayıyı bas
                    const formattedValue = config ? config.format(p.value) : p.value;

                    return (
                        <p key={p.dataKey} style={{margin: "2px 0", color: p.fill}}>
                            {p.name}: {formattedValue}
                        </p>
                    );
                })}
            </div>
        );
    };


    // Yeni Koyu Tema Input Stilleri
    const inputStyle: React.CSSProperties = {
        border: "1px solid #444444",
        borderRadius: 6,
        padding: "6px 10px",
        fontSize: 13,
        color: "#ffffff",
        background: "#2d2d2d",
        outline: "none",
    };

    return (
        <div style={{
            minHeight: "100vh",
            background: "#121212",
            padding: "2rem",
            fontFamily: "Arial, sans-serif",
            color: "#e0e0e0"
        }}>
            <div style={{maxWidth: 900, margin: "0 auto"}}>

                {/* Config bar (Koyu Tema) */}
                <div style={{
                    background: "#1e1e1e",
                    border: "1px solid #333333",
                    borderRadius: 12,
                    padding: "1.25rem",
                    marginBottom: "1.5rem"
                }}>
                    <div style={{display: "flex", flexWrap: "wrap", gap: "1rem", alignItems: "flex-end"}}>
                        <Field label="API base URL">
                            <input value={apiBase} onChange={e => setApiBase(e.target.value)}
                                   style={{...inputStyle, width: 190}}/>
                        </Field>
                        <Field label="Client ID">
                            <input type="number" value={clientId} onChange={e => setClientId(Number(e.target.value))}
                                   style={{...inputStyle, width: 65}}/>
                        </Field>
                        <Field label="Week start (day 1 of 7)">
                            <input type="date" value={weekStart} onChange={e => setWeekStart(e.target.value)}
                                   style={{...inputStyle, width: 155}}/>
                        </Field>
                        <Field label="Agency markup ×">
                            <input type="number" step="0.05" min="1" value={markup}
                                   onChange={e => setMarkup(Number(e.target.value))}
                                   style={{...inputStyle, width: 65}}/>
                        </Field>
                        <Field label="Client name (override)">
                            <input value={clientNameOverride} onChange={e => setClientNameOverride(e.target.value)}
                                   placeholder="From API" style={{...inputStyle, width: 150}}/>
                        </Field>
                        <Field label="Select Metrics to Display & Report">
                            <div style={{display: "flex", flexWrap: "wrap", gap: "8px", marginTop: "4px"}}>
                                {AVAILABLE_METRICS.map(m => (
                                    <label key={m.key} style={{
                                        display: "flex",
                                        alignItems: "center",
                                        gap: "4px",
                                        fontSize: "12px",
                                        cursor: "pointer",
                                        background: selectedMetrics.includes(m.key) ? "#0d47a1" : "#2d2d2d",
                                        padding: "4px 8px",
                                        borderRadius: "4px"
                                    }}>
                                        <input
                                            type="checkbox"
                                            checked={selectedMetrics.includes(m.key)}
                                            onChange={(e) => {
                                                if (e.target.checked) {
                                                    setSelectedMetrics([...selectedMetrics, m.key]);
                                                } else {
                                                    setSelectedMetrics(selectedMetrics.filter(x => x !== m.key));
                                                }
                                            }}
                                            style={{display: "none"}}
                                        />
                                        {m.label}
                                    </label>
                                ))}
                            </div>
                        </Field>
                        <button onClick={loadData} disabled={loading}
                                style={{
                                    background: "#0d47a1",
                                    color: "#fff",
                                    border: "none",
                                    borderRadius: 8,
                                    padding: "8px 18px",
                                    fontSize: 13,
                                    fontWeight: 600,
                                    cursor: loading ? "not-allowed" : "pointer",
                                    display: "flex",
                                    alignItems: "center",
                                    gap: 6,
                                    opacity: loading ? 0.7 : 1
                                }}>
                            <RefreshCw size={15} style={{animation: loading ? "spin 1s linear infinite" : "none"}}/>
                            {loading ? "Loading…" : "Load preview"}
                        </button>
                    </div>
                </div>

                {error && (
                    <div style={{
                        background: "#4c1d1d",
                        border: "1px solid #7a2828",
                        borderRadius: 8,
                        padding: "12px 16px",
                        color: "#f8a1a1",
                        fontSize: 13,
                        marginBottom: "1.5rem"
                    }}>
                        {error}
                    </div>
                )}

                {hasData && (
                    <>
                        <div style={{
                            background: "#1e1e1e",
                            border: "1px solid #333",
                            borderRadius: 12,
                            padding: "2rem",
                            marginBottom: "1rem"
                        }}>

                            {/* Header */}
                            <div style={{
                                borderBottom: "2px solid #2196f3",
                                paddingBottom: "1rem",
                                marginBottom: "1.5rem",
                                display: "flex",
                                justifyContent: "space-between",
                                alignItems: "flex-start"
                            }}>
                                <div>
                                    <p style={{
                                        fontSize: 20,
                                        fontWeight: 700,
                                        color: "#2196f3",
                                        margin: "0 0 4px"
                                    }}>Weekly Google Ads Report</p>
                                    <p style={{fontSize: 15, color: "#ffffff", margin: 0}}>{clientName}</p>
                                </div>
                                <div style={{fontSize: 11, color: "#aaaaaa", textAlign: "right"}}>
                                    {weekLabel}<br/>
                                    Generated: {new Date().toLocaleDateString("en-GB", {
                                    day: "2-digit",
                                    month: "short",
                                    year: "numeric"
                                })}
                                </div>
                            </div>

                            {markup !== 1 && (
                                <div style={{
                                    background: "#1a237e",
                                    borderLeft: "3px solid #2196f3",
                                    padding: "8px 12px",
                                    fontSize: 11,
                                    color: "#e3f2fd",
                                    marginBottom: "1.5rem"
                                }}>
                                    Agency markup of <strong>×{markup}</strong> applied to all spend figures.
                                </div>
                            )}

                            <SectionTitle>Weekly summary</SectionTitle>
                            <div style={{
                                display: "grid",
                                gridTemplateColumns: "repeat(3,1fr)",
                                gap: 10,
                                marginBottom: "1.5rem"
                            }}>
                                <SCard label="Total spend" value={fmtMoney(summary.spend)} color="#2196f3"/>
                                <SCard label="Total clicks" value={fmtNum(summary.clicks)} color="#4caf50"/>
                                <SCard label="Total impressions" value={fmtNum(summary.impressions)} color="#ffeb3b"/>
                                <SCard label="Avg CTR" value={fmtPct(summary.ctr)} color="#f44336"/>
                                <SCard label="Avg CPC" value={fmtMoney(summary.cpc)} tooltip={fmtMoney(summary.cpc, 4)}
                                       color="#b388ff"/>
                                <SCard label="Avg CPM" value={fmtMoney(summary.cpm)} tooltip={fmtMoney(summary.cpm, 4)}
                                       color="#00e5ff"/>
                            </div>

                            <SectionTitle>Daily breakdown</SectionTitle>
                            <table style={{
                                width: "100%",
                                borderCollapse: "collapse",
                                marginBottom: "1.5rem",
                                fontSize: 11,
                                tableLayout: "fixed"
                            }}>
                                <thead>
                                <tr>
                                    {/* İlk sütun her zaman Tarih kalıyor */}
                                    <Th>Date</Th>
                                    {/* Seçilen metriklere göre dinamik başlıklar oluşturuluyor */}
                                    {selectedMetrics.map(mKey => {
                                        const config = AVAILABLE_METRICS.find(m => m.key === mKey);
                                        return <Th key={mKey}>{config?.label || mKey}</Th>;
                                    })}
                                </tr>
                                </thead>
                                <tbody>
                                {allDays.map((d, i) => {
                                    const v = dailyMap[d];

                                    // Bu güne ait hesaplanmış (oran) metrikleri anlık türetiyoruz
                                    const rawSpend = (v.spend / markup); // Orijinal harcama (ihtiyaç halinde)
                                    const calculatedForDay: Record<string, number> = {
                                        ctr: v.impressions > 0 ? (v.clicks / v.impressions) * 100 : 0,
                                        cpc: v.clicks > 0 ? v.spend / v.clicks : 0,
                                        cpm: v.impressions > 0 ? (v.spend / v.impressions) * 1000 : 0,
                                        totalViews: (v as any).totalViews || 0,
                                        totalConversions: (v as any).totalConversions || 0,
                                        conversionValue: (v as any).conversionValue || 0,
                                        cpv: (v as any).totalViews > 0 ? v.spend / (v as any).totalViews : 0,
                                        cpa: (v as any).totalConversions > 0 ? v.spend / (v as any).totalConversions : 0,
                                        roas: v.spend > 0 ? ((v as any).conversionValue || 0) / v.spend : 0,
                                    };

                                    // Api'den gelen ham değerler ile hesaplananları birleştiriyoruz
                                    const allRowData = {
                                        totalSpend: v.spend,
                                        totalClicks: v.clicks,
                                        totalImpressions: v.impressions,
                                        ...calculatedForDay
                                    };

                                    const label = parseLocalDate(d).toLocaleDateString("en-GB", {
                                        weekday: "short",
                                        day: "2-digit",
                                        month: "short",
                                        year: "numeric"
                                    });

                                    return (
                                        <tr key={d} style={{background: i % 2 === 1 ? "#252525" : "#1e1e1e"}}>
                                            <Td>{label}</Td>
                                            {/* Seçilen metriklere göre hücreleri basıyoruz */}
                                            {selectedMetrics.map(mKey => {
                                                const config = AVAILABLE_METRICS.find(m => m.key === mKey);
                                                const rawValue = allRowData[mKey as keyof typeof allRowData] || 0;
                                                const formatted = config ? config.format(rawValue) : rawValue.toString();
                                                return (
                                                    <Td key={mKey} title={formatted}>
                                                        {formatted}
                                                    </Td>
                                                );
                                            })}
                                        </tr>
                                    );
                                })}
                                </tbody>
                            </table>

                            <SectionTitle>Top campaigns by spend</SectionTitle>
                            <table style={{
                                width: "100%",
                                borderCollapse: "collapse",
                                marginBottom: "1.5rem",
                                fontSize: 11,
                                tableLayout: "fixed"
                            }}>
                                <thead>
                                <tr>
                                    <Th>Campaign</Th>
                                    {selectedMetrics.map(mKey => {
                                        const config = AVAILABLE_METRICS.find(m => m.key === mKey);
                                        return <Th key={mKey}>{config?.label || mKey}</Th>;
                                    })}
                                </tr>
                                </thead>
                                <tbody>
                                {campRows.map(([name, v], i) => {
                                    const calculatedForCamp: Record<string, number> = {
                                        ctr: v.impressions > 0 ? (v.clicks / v.impressions) * 100 : 0,
                                        cpc: v.clicks > 0 ? v.spend / v.clicks : 0,
                                        cpm: v.impressions > 0 ? (v.spend / v.impressions) * 1000 : 0,
                                        totalViews: (v as any).totalViews || 0,
                                        totalConversions: v.conversions,
                                        conversionValue: (v as any).conversionValue || 0,
                                        cpv: (v as any).totalViews > 0 ? v.spend / (v as any).totalViews : 0,
                                        cpa: v.conversions > 0 ? v.spend / v.conversions : 0,
                                        roas: v.spend > 0 ? ((v as any).conversionValue || 0) / v.spend : 0,
                                    };

                                    const allCampData = {
                                        totalSpend: v.spend,
                                        totalClicks: v.clicks,
                                        totalImpressions: v.impressions,
                                        ...calculatedForCamp
                                    };

                                    return (
                                        <tr key={name} style={{background: i % 2 === 1 ? "#252525" : "#1e1e1e"}}>
                                            <Td>{name}</Td>
                                            {selectedMetrics.map(mKey => {
                                                const config = AVAILABLE_METRICS.find(m => m.key === mKey);
                                                const rawValue = allCampData[mKey as keyof typeof allCampData] || 0;
                                                const formatted = config ? config.format(rawValue) : rawValue.toString();
                                                return <Td key={mKey}>{formatted}</Td>;
                                            })}
                                        </tr>
                                    );
                                })}
                                </tbody>
                            </table>

                            <div style={{
                                marginTop: "1.5rem",
                                paddingTop: "1rem",
                                borderTop: "1px solid #333",
                                fontSize: 10,
                                color: "#666",
                                textAlign: "center"
                            }}>
                                Generated by Marketing Analytics Platform · {new Date().toLocaleDateString("en-GB", {
                                day: "2-digit",
                                month: "short",
                                year: "numeric"
                            })}
                            </div>
                        </div>

                        <div style={{display: "flex", justifyContent: "flex-end"}}>
                            <button onClick={handleDownload} disabled={downloading}
                                    style={{
                                        background: "#0d47a1",
                                        color: "#fff",
                                        border: "none",
                                        borderRadius: 8,
                                        padding: "10px 22px",
                                        fontSize: 13,
                                        fontWeight: 600,
                                        cursor: downloading ? "not-allowed" : "pointer",
                                        display: "flex",
                                        alignItems: "center",
                                        gap: 8,
                                        opacity: downloading ? 0.7 : 1
                                    }}>
                                {downloading ? <Loader size={15} style={{animation: "spin 1s linear infinite"}}/> :
                                    <Download size={15}/>}
                                {downloading ? "Generating PDF…" : "Download PDF"}
                            </button>
                        </div>
                    </>
                )}
            </div>
            <style>{`@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }`}</style>
        </div>
    );
}

function Field({label, children}: { label: string; children: React.ReactNode }) {
    return (
        <div>
            <label style={{
                display: "block",
                fontSize: 12,
                color: "#e0e0e0",
                fontWeight: 600,
                marginBottom: 4
            }}>{label}</label>
            {children}
        </div>
    );
}

function SectionTitle({children}: { children: React.ReactNode }) {
    return <p style={{
        fontSize: 13,
        fontWeight: 700,
        color: "#ffffff",
        margin: "0 0 10px",
        borderLeft: "3px solid #2196f3",
        paddingLeft: 8
    }}>{children}</p>;
}

function SCard({label, value, color, tooltip}: { label: string; value: string; color: string; tooltip?: string }) {
    return (
        <div title={tooltip} style={{border: "1px solid #333333", background: "#151515", borderRadius: 8, padding: 12}}>
            <p style={{fontSize: 10, color: "#aaaaaa", fontWeight: 600, margin: "0 0 4px"}}>{label}</p>
            <p style={{fontSize: 18, fontWeight: 700, color, margin: 0}}>{value}</p>
        </div>
    );
}

function Th({children}: { children: React.ReactNode }) {
    return <th style={{
        background: "#2196f3",
        color: "#fff",
        padding: "7px 8px",
        textAlign: "left",
        fontWeight: 600,
        fontSize: 11
    }}>{children}</th>
}

function Td({children, title}: { children: React.ReactNode; title?: string }) {
    return <td title={title} style={{
        padding: "6px 8px",
        borderBottom: "1px solid #333",
        color: "#e0e0e0",
        fontSize: 11
    }}>{children}</td>;
}