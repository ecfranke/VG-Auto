import 'server-only'
import { cookies, headers } from 'next/headers'

export type SiteLang = 'en' | 'zh'
export const SITE_LANG_COOKIE = 'site_lang'

const en = {
  lang: 'en' as SiteLang,
  htmlTitle: 'VG Auto — workshop management',
  signIn: 'Sign in',
  switchTo: '中文',
  switchToLang: 'zh' as SiteLang,
  heroEyebrow: 'Workshop management for professional repair shops',
  heroTitle: 'Precision for every job.',
  heroSub: 'Estimates, repairs, parts and invoices — engineered like the cars you fix.',
  heroCta: 'Sign in',
  heroMore: 'Scroll to see how it works',
  story: [
    { kicker: 'Inspection', title: 'Every vehicle, every detail.', text: 'Open a work order for the client and the car in seconds. The full history is one search away.' },
    { kicker: 'Diagnosis', title: 'Take it apart. Line by line.', text: 'Tyre, rim, disc, caliper: every part and every hour of labour goes on the estimate, priced before tax.' },
    { kicker: 'Estimate', title: 'Approved before you start.', text: 'The client gets a clean PDF by email and signs it on their phone. No printing, no phone calls.' },
    { kicker: 'Invoice', title: 'Back on the road. Invoiced.', text: 'One click turns the finished job into an invoice with GST, HST, PST or QST — sent by email.' },
  ],
  parts: { tyre: 'Tyre', rim: 'Rim', disc: 'Brake disc', caliper: 'Caliper & pads', hub: 'Wheel hub' },
  estimateTitle: 'Estimate',
  estimateLines: [
    { name: 'Brake disc, front (2)', price: '$212.00' },
    { name: 'Brake pads, front axle', price: '$148.00' },
    { name: 'Wheel balancing', price: '$60.00' },
    { name: 'Labour 1.5 h', price: '$165.00' },
  ],
  estimateTax: 'GST 5% · PST 7%',
  estimateTaxAmount: '$70.20',
  estimateTotal: 'Total',
  estimateTotalAmount: '$655.20',
  signed: 'Signed by client',
  invoiced: 'Invoice RP_TF_2019_HC sent · paid',
  featuresKicker: 'Everything in one place',
  featuresTitle: 'Built around how a workshop really works.',
  features: [
    { title: 'Work orders', text: 'Estimates, repair jobs, parts and labour together. An accepted estimate becomes a repair job in one click.' },
    { title: 'Online signing', text: 'Clients review every line of the estimate and sign it on their phone.' },
    { title: 'PDF by email', text: 'Estimates and invoices named after the job, sent straight from the app.' },
    { title: 'Clients & vehicles', text: 'Every customer and car with their estimates, invoices and work done.' },
    { title: 'Inventory', text: 'Spare parts with storage locations and prices, added to a job from the shelf.' },
    { title: 'Taxes by province', text: 'GST, HST, PST and QST, prices before tax, every tax shown on the documents.' },
  ],
  stats: [
    { value: '4', label: 'steps from estimate to invoice' },
    { value: '1', label: 'click from accepted estimate to repair job' },
    { value: '0', label: 'paper forms' },
  ],
  ctaTitle: 'Your workshop. Organised.',
  ctaText: 'Sign in with the account your administrator created for you.',
  footer: 'V. G. Global Solution Canada Inc. All rights reserved.',
}

const zh: typeof en = {
  lang: 'zh',
  htmlTitle: 'VG Auto — 汽修厂管理系统',
  signIn: '登录',
  switchTo: 'English',
  switchToLang: 'en',
  heroEyebrow: '为专业汽修厂打造的管理系统',
  heroTitle: '每一单，都精准到位。',
  heroSub: '报价、维修、配件与发票——像对待每一辆车一样严谨。',
  heroCta: '登录',
  heroMore: '向下滚动，了解工作流程',
  story: [
    { kicker: '检查', title: '每一辆车，每一个细节。', text: '几秒钟为客户和车辆开好工单，所有历史记录一搜即得。' },
    { kicker: '诊断', title: '逐件拆解，逐行记录。', text: '轮胎、轮毂、刹车盘、卡钳——每个配件和每小时工时都写进报价单，价格均为税前。' },
    { kicker: '报价', title: '动工之前，客户已确认。', text: '客户通过邮件收到清晰的 PDF 报价单，在手机上直接签字。无需打印，无需来回电话。' },
    { kicker: '发票', title: '车辆交付，发票随即发出。', text: '维修完成后一键生成发票，自动计算 GST、HST、PST 或 QST，并通过邮件发送。' },
  ],
  parts: { tyre: '轮胎', rim: '轮毂', disc: '刹车盘', caliper: '卡钳与刹车片', hub: '轮轴' },
  estimateTitle: '报价单',
  estimateLines: [
    { name: '前刹车盘（2 个）', price: '$212.00' },
    { name: '前轴刹车片', price: '$148.00' },
    { name: '车轮动平衡', price: '$60.00' },
    { name: '工时 1.5 小时', price: '$165.00' },
  ],
  estimateTax: 'GST 5% · PST 7%',
  estimateTaxAmount: '$70.20',
  estimateTotal: '合计',
  estimateTotalAmount: '$655.20',
  signed: '客户已签字',
  invoiced: '发票 RP_TF_2019_HC 已发送 · 已付款',
  featuresKicker: '一站式管理',
  featuresTitle: '按照修车厂真实的工作方式设计。',
  features: [
    { title: '工单', text: '报价、维修、配件和工时集中管理。报价被接受后，一键转为维修单。' },
    { title: '在线签字', text: '客户逐行查看报价，在手机上签字确认。' },
    { title: 'PDF 邮件', text: '报价单和发票以工单命名，直接从系统发送。' },
    { title: '客户与车辆', text: '每位客户、每辆车的报价、发票和维修记录一目了然。' },
    { title: '库存', text: '配件带库位和价格，从货架直接加入工单。' },
    { title: '按省份计税', text: 'GST、HST、PST、QST，税前定价，单据上列出每一项税。' },
  ],
  stats: [
    { value: '4', label: '步，从报价到发票' },
    { value: '1', label: '次点击，报价转维修单' },
    { value: '0', label: '张纸质表格' },
  ],
  ctaTitle: '让你的修车厂井井有条。',
  ctaText: '使用管理员为你创建的账号登录。',
  footer: 'V. G. Global Solution Canada Inc. 版权所有。',
}

export type SiteDictionary = typeof en

/** The language of the landing page: chosen with the switch (cookie), otherwise the browser's language. */
export async function siteDictionary(): Promise<SiteDictionary> {
  const saved = (await cookies()).get(SITE_LANG_COOKIE)?.value
  if (saved === 'zh') return zh
  if (saved === 'en') return en
  const accept = (await headers()).get('accept-language') ?? ''
  return accept.toLowerCase().startsWith('zh') ? zh : en
}
