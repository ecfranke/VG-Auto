'use client'

import { createContext, useContext } from 'react'
import { DEFAULT_CURRENCY } from '@/_lib/shared/money'

/** Currency of the activity (offer or repair job) being shown. */
export const CurrencyContext = createContext<string>(DEFAULT_CURRENCY)
export const useCurrency = () => useContext(CurrencyContext)
