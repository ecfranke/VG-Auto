-- An offer deleted after it was issued left its estimate behind, still holding its number ("15-1"). The next offer of
-- that work got the same number and issuing it failed: duplicate key "uq_estimate_company_number" (500).
-- Deleting an offer now deletes its unsent estimate; estimates left behind before keep their record under a freed number.
UPDATE estimate e
   SET e.number = CONCAT(e.number, '~', LEFT(e.id, 8))
 WHERE NOT EXISTS (SELECT 1 FROM offer o WHERE o.estimateid = e.id)
   AND e.number NOT LIKE '%~%';
