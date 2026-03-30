import { createApi, fetchBaseQuery } from "@reduxjs/toolkit/query/react";

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "";

export const api = createApi({
  reducerPath: "api",
  baseQuery: fetchBaseQuery({
    baseUrl: baseUrl || undefined,
    prepareHeaders: (headers) => {
      const token = localStorage.getItem("access_token");
      if (token) headers.set("Authorization", `Bearer ${token}`);
      return headers;
    },
  }),
  tagTypes: ["Me", "Dashboard", "Subscriptions", "Alerts", "Recurring"],
  endpoints: (build) => ({
    getMe: build.query<
      {
        userId: string;
        accountId: string;
        email: string | null;
        uiCulture: string;
        defaultCurrency: string;
      },
      void
    >({
      query: () => "/api/me",
      providesTags: ["Me"],
    }),
    getDashboardSummary: build.query<
      {
        monthlySaaSSpendEstimate: number;
        activeSubscriptionCount: number;
        openAlertCount: number;
        pendingConfirmationCount: number;
        potentialDuplicateSpend: number;
      },
      void
    >({
      query: () => "/api/dashboard/summary",
      providesTags: ["Dashboard"],
    }),
    getSubscriptions: build.query<
      Array<{
        id: string;
        vendorName: string;
        normalizedMerchant: string;
        recurringType: number;
        subscriptionConfidenceScore: number;
        classificationReason: string;
        isSubscriptionCandidate: boolean;
        averageAmount: number;
        currency: string;
        cadence: number;
        lastChargeDate: string;
        nextExpectedChargeDate: string;
        status: number;
        patternConfidenceScore: number;
        ownerUserId: string | null;
        ownerName: string | null;
        ownerEmail: string | null;
        reviewStatus: number;
        lastConfirmedInUseAt: string | null;
        lastReviewRequestedAt: string | null;
        nextReviewDate: string | null;
        usageConfidenceScore: number | null;
      }>,
      boolean | void
    >({
      query: (likelySaaSMediaOnly) =>
        likelySaaSMediaOnly === true
          ? "/api/subscriptions?likelySaaSMediaOnly=true"
          : "/api/subscriptions",
      providesTags: ["Subscriptions"],
    }),
    getRecurringReview: build.query<
      Array<{
        id: string;
        vendorName: string;
        normalizedMerchant: string;
        recurringType: number;
        subscriptionConfidenceScore: number;
        classificationReason: string;
        patternConfidenceScore: number;
        cadence: number;
        averageAmount: number;
        currency: string;
        lastChargeDate: string;
        nextExpectedChargeDate: string;
        status: number;
        uiLabel: string;
      }>,
      { includeNonSubscription?: boolean } | void
    >({
      query: (arg) => {
        const inc =
          arg && typeof arg === "object" && arg.includeNonSubscription === true
            ? "?includeNonSubscription=true"
            : "";
        return `/api/recurring/review${inc}`;
      },
      providesTags: ["Recurring"],
    }),
    getAlerts: build.query<
      Array<{
        id: string;
        subscriptionId: string | null;
        alertType: number;
        severity: number;
        title: string;
        message: string;
        isRead: boolean;
        alertStatus: number;
        responseType: number | null;
        respondedAt: string | null;
        respondedByUserId: string | null;
        notes: string | null;
        createdAt: string;
      }>,
      void
    >({
      query: () => "/api/alerts",
      providesTags: ["Alerts"],
    }),
    importTransactions: build.mutation<
      { importId: string },
      { fileName: string; csvContent: string }
    >({
      query: (body) => ({
        url: "/api/transactions/import",
        method: "POST",
        body,
      }),
      invalidatesTags: ["Dashboard", "Subscriptions", "Alerts", "Recurring"],
    }),
    patchSubscriptionOwner: build.mutation<
      unknown,
      { id: string; ownerName?: string | null; ownerEmail?: string | null; ownerUserId?: string | null }
    >({
      query: ({ id, ...body }) => ({
        url: `/api/subscriptions/${id}/owner`,
        method: "PATCH",
        body,
      }),
      invalidatesTags: ["Subscriptions", "Alerts", "Dashboard"],
    }),
    requestSubscriptionReview: build.mutation<void, { id: string }>({
      query: ({ id }) => ({
        url: `/api/subscriptions/${id}/request-review`,
        method: "POST",
      }),
      invalidatesTags: ["Subscriptions", "Alerts", "Dashboard"],
    }),
    classifyRecurringCandidate: build.mutation<
      void,
      { id: string; action: "confirmSubscription" | "dismiss"; recurringType?: number }
    >({
      query: ({ id, action, recurringType }) => ({
        url: `/api/recurring/candidates/${id}/classify`,
        method: "POST",
        body: { action, recurringType: recurringType ?? null },
      }),
      invalidatesTags: ["Recurring", "Subscriptions", "Dashboard", "Alerts"],
    }),
    respondToAlert: build.mutation<
      {
        alertId: string;
        alertStatus: number;
        subscriptionId: string | null;
        subscriptionReviewStatus: number;
        nextReviewDate: string | null;
      },
      { id: string; response: number; notes?: string | null }
    >({
      query: ({ id, ...body }) => ({
        url: `/api/alerts/${id}/respond`,
        method: "POST",
        body: { response: body.response, notes: body.notes ?? null },
      }),
      invalidatesTags: ["Alerts", "Subscriptions", "Dashboard"],
    }),
    resetAccountData: build.mutation<void, void>({
      query: () => ({
        url: "/api/account/reset-data",
        method: "POST",
        body: { confirm: true },
      }),
      invalidatesTags: ["Me", "Dashboard", "Subscriptions", "Alerts", "Recurring"],
    }),
  }),
});

export const {
  useGetMeQuery,
  useGetDashboardSummaryQuery,
  useGetSubscriptionsQuery,
  useGetRecurringReviewQuery,
  useGetAlertsQuery,
  useImportTransactionsMutation,
  usePatchSubscriptionOwnerMutation,
  useRequestSubscriptionReviewMutation,
  useClassifyRecurringCandidateMutation,
  useRespondToAlertMutation,
  useResetAccountDataMutation,
} = api;
