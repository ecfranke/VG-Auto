-- Estimates of deleted work were left behind (the offer was deleted, the estimate was not). A new work that got the
-- deleted work's number then could not issue its offer: "duplicate key ... uq_estimate_company_number".
-- Work with a sent offer cannot be deleted, so these are unsent drafts nothing refers to.
CREATE TEMP TABLE orphan_estimate AS
SELECT e.id FROM domain.estimate e WHERE NOT EXISTS (SELECT 1 FROM domain.offer o WHERE o.estimateid = e.id);

DELETE FROM domain.pricingline WHERE pricingid IN (SELECT id FROM orphan_estimate);
DELETE FROM domain.estimate WHERE id IN (SELECT id FROM orphan_estimate);
DELETE FROM domain.pricing WHERE id IN (SELECT id FROM orphan_estimate);
DROP TABLE orphan_estimate;
